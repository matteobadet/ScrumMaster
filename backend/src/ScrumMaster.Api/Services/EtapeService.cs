using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record AvancerEtapeResult(bool BoardFerme, Guid? NouvelleEtapeId);

/// <summary>
/// Composition et progression de la séquence d'étapes d'un board — voir
/// specs/006-systeme-extensions-etapes. Porte aussi la résolution de thème (déplacée depuis
/// BoardService : le thème appartient désormais à l'étape, pas au board — research.md#1).
/// </summary>
public class EtapeService(ScrumMasterDbContext db)
{
    /// <summary>
    /// Construit la séquence d'étapes d'un nouveau board. Sans séquence explicite, repli sur une
    /// seule étape "Colonnes et post-its" (FR-014, comportement historique de
    /// specs/001-retro-board-base).
    /// </summary>
    public async Task<List<Etape>> ConstruireSequenceAsync(
        IReadOnlyList<EtapeRequestDto>? etapesDemandees,
        Guid? themeIdSimple,
        ThemePersonnaliseDto? themePersonnaliseSimple
    )
    {
        if (etapesDemandees is null)
        {
            var theme = await ResolveThemeAsync(themeIdSimple, themePersonnaliseSimple);
            return [CreerEtapeColonnesEtPostIts(0, theme, StatutEtape.Active)];
        }

        if (etapesDemandees.Count == 0)
        {
            throw new DomainValidationException("La séquence d'étapes doit comporter au moins une étape.");
        }

        var etapes = new List<Etape>();
        for (var i = 0; i < etapesDemandees.Count; i++)
        {
            var statut = i == 0 ? StatutEtape.Active : StatutEtape.AVenir;
            etapes.Add(await ConstruireEtapeAsync(etapesDemandees[i], i, statut));
        }

        return etapes;
    }

    private async Task<Etape> ConstruireEtapeAsync(EtapeRequestDto demande, int ordre, StatutEtape statut)
    {
        if (!Enum.TryParse<TypeEtape>(demande.Type, out var type))
        {
            throw new DomainValidationException($"Type d'étape \"{demande.Type}\" non reconnu.");
        }

        return type switch
        {
            TypeEtape.ColonnesEtPostIts => CreerEtapeColonnesEtPostIts(
                ordre,
                await ResolveThemeAsync(demande.ThemeId, demande.ThemePersonnalise),
                statut
            ),
            TypeEtape.MiniJeu => await CreerEtapeMiniJeuAsync(ordre, demande.MiniJeuCatalogueId, statut),
            TypeEtape.PollPersonnalise => CreerEtapePollPersonnalise(ordre, demande.Question, demande.Options, statut),
            _ => throw new DomainValidationException($"Type d'étape \"{demande.Type}\" non reconnu."),
        };
    }

    private static Etape CreerEtapeColonnesEtPostIts(int ordre, Theme theme, StatutEtape statut) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = TypeEtape.ColonnesEtPostIts,
            Ordre = ordre,
            Statut = statut,
            ThemeId = theme.Id,
            Theme = theme,
        };

    private async Task<Etape> CreerEtapeMiniJeuAsync(int ordre, Guid? miniJeuCatalogueId, StatutEtape statut)
    {
        if (miniJeuCatalogueId is null || !await db.MiniJeuxCatalogue.AnyAsync(m => m.Id == miniJeuCatalogueId))
        {
            throw new DomainValidationException("Un mini-jeu du catalogue doit être choisi pour une étape de type Mini-jeu.");
        }

        return new Etape
        {
            Id = Guid.NewGuid(),
            Type = TypeEtape.MiniJeu,
            Ordre = ordre,
            Statut = statut,
            MiniJeuCatalogueId = miniJeuCatalogueId,
        };
    }

    private static Etape CreerEtapePollPersonnalise(int ordre, string? question, IReadOnlyList<string>? options, StatutEtape statut)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new DomainValidationException("Une étape de poll personnalisé doit avoir une question.");
        }

        var optionsNonVides = (options ?? []).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        if (optionsNonVides.Count < 2)
        {
            throw new DomainValidationException("Une étape de poll personnalisé doit avoir au moins deux options de réponse.");
        }

        var etape = new Etape
        {
            Id = Guid.NewGuid(),
            Type = TypeEtape.PollPersonnalise,
            Ordre = ordre,
            Statut = statut,
            Question = question.Trim(),
        };

        etape.Options = optionsNonVides
            .Select((texte, index) => new OptionPollPersonnalise { Id = Guid.NewGuid(), EtapeId = etape.Id, Texte = texte.Trim(), Ordre = index })
            .ToList();

        return etape;
    }

    /// <summary>Résout l'étape actuellement active d'un board déjà chargé avec ses étapes.</summary>
    public Etape ObtenirEtapeActive(Board board)
    {
        var active = board.Etapes.FirstOrDefault(e => e.Statut == StatutEtape.Active);
        if (active is null)
        {
            throw new DomainValidationException("Ce board n'a aucune étape active.");
        }

        return active;
    }

    /// <summary>Clôt l'étape active ; active la suivante si elle existe, sinon clôture le board entier (FR-005, FR-006).</summary>
    public async Task<AvancerEtapeResult> AvancerEtapeAsync(Guid boardId, Guid callerParticipantId)
    {
        var board = await db.Boards.Include(b => b.Etapes).FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var caller = await db.Participants.FirstOrDefaultAsync(p => p.Id == callerParticipantId && p.BoardId == boardId);
        if (caller is null)
        {
            throw new DomainNotFoundException($"Participant {callerParticipantId} introuvable sur ce board.");
        }

        if (caller.Role != ParticipantRole.Facilitateur)
        {
            throw new DomainForbiddenException("Seul le facilitateur peut faire avancer le board.");
        }

        BoardClosureGuard.EnsureActif(board);

        var etapeActive = ObtenirEtapeActive(board);
        etapeActive.Statut = StatutEtape.Terminee;

        var suivante = board.Etapes.Where(e => e.Ordre > etapeActive.Ordre).OrderBy(e => e.Ordre).FirstOrDefault();
        if (suivante is not null)
        {
            suivante.Statut = StatutEtape.Active;
            await db.SaveChangesAsync();
            return new AvancerEtapeResult(BoardFerme: false, NouvelleEtapeId: suivante.Id);
        }

        board.Statut = BoardStatut.Cloture;
        await db.SaveChangesAsync();
        return new AvancerEtapeResult(BoardFerme: true, NouvelleEtapeId: null);
    }

    /// <summary>Résolution de thème (déplacée depuis BoardService, specs/001-retro-board-base — le thème appartient à l'étape).</summary>
    public async Task<Theme> ResolveThemeAsync(Guid? themeId, ThemePersonnaliseDto? themePersonnalise)
    {
        if (themeId is { } id)
        {
            var source = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.Id == id);
            if (source is null)
            {
                throw new DomainValidationException($"Thème {id} introuvable.");
            }

            return CopyTheme(source.Nom, source.Icone, source.Contexte, source.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
        }

        if (themePersonnalise is not null)
        {
            var colonnes = themePersonnalise.Colonnes.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            if (colonnes.Count == 0)
            {
                throw new DomainValidationException("Un thème doit comporter au moins une colonne.");
            }

            if (themePersonnalise.Icone?.Length > 50)
            {
                throw new DomainValidationException("L'icône du thème ne doit pas dépasser 50 caractères.");
            }

            if (themePersonnalise.Contexte?.Length > 500)
            {
                throw new DomainValidationException("Le contexte du thème ne doit pas dépasser 500 caractères.");
            }

            return CopyTheme(themePersonnalise.Nom, themePersonnalise.Icone, themePersonnalise.Contexte, colonnes);
        }

        var defaut = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.EstParDefaut);
        if (defaut is null)
        {
            throw new DomainValidationException("Aucun thème par défaut n'est configuré.");
        }

        return CopyTheme(defaut.Nom, defaut.Icone, defaut.Contexte, defaut.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
    }

    private static Theme CopyTheme(string nom, string? icone, string? contexte, IEnumerable<string> colonnes)
    {
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Icone = icone,
            Contexte = contexte,
            EstPredefini = false,
            EstParDefaut = false,
        };

        theme.Colonnes = colonnes
            .Select((intitule, index) => new Colonne { Id = Guid.NewGuid(), ThemeId = theme.Id, Intitule = intitule, Ordre = index })
            .ToList();

        return theme;
    }
}
