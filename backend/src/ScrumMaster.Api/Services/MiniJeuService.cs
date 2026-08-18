using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record RepondreMiniJeuResult(Guid ParticipantId, string NomAffiche, string Reponse);

public record LettreProposeeResult(string Lettre, bool Correcte, string NomAffiche);

public record ProposerLettreResult(
    string Lettre,
    bool Correcte,
    IReadOnlyList<string?> MotMasque,
    IReadOnlyList<LettreProposeeResult> LettresProposees,
    int EssaisRestants,
    int MaxEssais,
    string Etat,
    string? MotComplet
);

public record DefinirLienExterneResult(string Nom, string Url);

/// <summary>
/// Réponse à une étape de type Mini-jeu (US2, specs/006-systeme-extensions-etapes). Le mini-jeu
/// concret ("Météo d'équipe" ou "ROTI") est résolu via <see cref="MiniJeuCatalogue.TypeInterne"/>
/// (specs/008-roti-mini-jeu, research.md#4).
/// </summary>
public class MiniJeuService(ScrumMasterDbContext db)
{
    public async Task<RepondreMiniJeuResult> RepondreAsync(Guid boardId, Guid etapeId, Guid participantId, string reponse)
    {
        var etape = await GetEtapeActiveAsync(boardId, etapeId);
        var participant = await GetParticipantAsync(boardId, participantId);

        return etape.MiniJeuCatalogue?.TypeInterne switch
        {
            "roti" => await RepondreRotiAsync(etapeId, participantId, participant, reponse),
            _ => await RepondreMeteoAsync(etapeId, participantId, participant, reponse),
        };
    }

    private async Task<RepondreMiniJeuResult> RepondreMeteoAsync(Guid etapeId, Guid participantId, Participant participant, string reponse)
    {
        if (!Enum.TryParse<HumeurMeteo>(reponse, ignoreCase: true, out var humeur))
        {
            throw new DomainValidationException($"Réponse \"{reponse}\" non reconnue pour ce mini-jeu.");
        }

        var existante = await db.ReponsesMeteoEquipe.FirstOrDefaultAsync(r => r.EtapeId == etapeId && r.ParticipantId == participantId);
        if (existante is null)
        {
            db.ReponsesMeteoEquipe.Add(
                new ReponseMeteoEquipe
                {
                    EtapeId = etapeId,
                    ParticipantId = participantId,
                    Humeur = humeur,
                    DateReponse = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            existante.Humeur = humeur;
            existante.DateReponse = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new RepondreMiniJeuResult(participantId, participant.NomAffiche, humeur.ToString());
    }

    private async Task<RepondreMiniJeuResult> RepondreRotiAsync(Guid etapeId, Guid participantId, Participant participant, string reponse)
    {
        if (!Enum.TryParse<NiveauRoti>(reponse, ignoreCase: true, out var niveau))
        {
            throw new DomainValidationException($"Réponse \"{reponse}\" non reconnue pour ce mini-jeu.");
        }

        var existante = await db.ReponsesRoti.FirstOrDefaultAsync(r => r.EtapeId == etapeId && r.ParticipantId == participantId);
        if (existante is null)
        {
            db.ReponsesRoti.Add(
                new ReponseRoti
                {
                    EtapeId = etapeId,
                    ParticipantId = participantId,
                    Niveau = niveau,
                    DateReponse = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            existante.Niveau = niveau;
            existante.DateReponse = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new RepondreMiniJeuResult(participantId, participant.NomAffiche, niveau.ToString());
    }

    /// <summary>
    /// Propose une lettre pour la partie de Pendu de cette étape (US1, specs/011-pendu-lien-
    /// externe). Journal partagé append-only, distinct de <see cref="RepondreAsync"/>
    /// (research.md#1) : renvoie <c>null</c> (no-op, FR-006) si la lettre a déjà été proposée par
    /// n'importe qui.
    /// </summary>
    public async Task<ProposerLettreResult?> ProposerLettrePenduAsync(Guid boardId, Guid etapeId, Guid callerParticipantId, string lettre)
    {
        var etape = await GetEtapeActiveAsync(boardId, etapeId);
        if (etape.MiniJeuCatalogue?.TypeInterne != "pendu")
        {
            throw new DomainValidationException("Cette étape n'est pas un mini-jeu Pendu.");
        }

        await GetParticipantAsync(boardId, callerParticipantId);

        if (string.IsNullOrEmpty(lettre) || lettre.Length != 1 || !char.IsLetter(lettre[0]))
        {
            throw new DomainValidationException("La lettre proposée doit être un unique caractère alphabétique.");
        }

        var lettreNormalisee = char.ToUpperInvariant(lettre[0]);

        var lettresExistantes = await db.LettresProposeesPendu.Where(l => l.EtapeId == etapeId).ToListAsync();

        var (_, _, etatAvant, _) = PenduGameState.Calculer(etape.MotAPendu!, lettresExistantes.Select(l => (l.Lettre, l.Correcte)));
        if (etatAvant != "EnCours")
        {
            throw new DomainValidationException("Cette partie de Pendu est déjà terminée.");
        }

        if (lettresExistantes.Any(l => l.Lettre == lettreNormalisee))
        {
            return null;
        }

        var correcte = etape.MotAPendu!.Any(c => char.ToUpperInvariant(c) == lettreNormalisee);
        db.LettresProposeesPendu.Add(
            new LettreProposeePendu
            {
                EtapeId = etapeId,
                Lettre = lettreNormalisee,
                Correcte = correcte,
                ParticipantProposantId = callerParticipantId,
                DateProposition = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();

        var lettresAvecParticipant = await db
            .LettresProposeesPendu.Where(l => l.EtapeId == etapeId)
            .Include(l => l.ParticipantProposant)
            .OrderBy(l => l.DateProposition)
            .ToListAsync();

        var (motMasque, essaisRestants, etat, motComplet) = PenduGameState.Calculer(
            etape.MotAPendu!,
            lettresAvecParticipant.Select(l => (l.Lettre, l.Correcte))
        );

        return new ProposerLettreResult(
            lettreNormalisee.ToString(),
            correcte,
            motMasque,
            lettresAvecParticipant.Select(l => new LettreProposeeResult(l.Lettre.ToString(), l.Correcte, l.ParticipantProposant?.NomAffiche ?? string.Empty)).ToList(),
            essaisRestants,
            PenduGameState.MaxEssais,
            etat,
            motComplet
        );
    }

    /// <summary>
    /// Définit ou remplace le lien externe d'une étape, en direct pendant qu'elle est active (US2,
    /// specs/011-pendu-lien-externe) — réservé au facilitateur, même pattern que
    /// <c>BoardService.ChangeThemeAsync</c> (research.md#5).
    /// </summary>
    public async Task<DefinirLienExterneResult> DefinirLienExterneAsync(Guid boardId, Guid etapeId, Guid callerParticipantId, string nom, string url)
    {
        var etape = await GetEtapeActiveAsync(boardId, etapeId);
        if (etape.MiniJeuCatalogue?.TypeInterne != "lien-externe")
        {
            throw new DomainValidationException("Cette étape n'est pas un mini-jeu Lien externe.");
        }

        var caller = await GetParticipantAsync(boardId, callerParticipantId);
        if (caller.Role != ParticipantRole.Facilitateur)
        {
            throw new DomainForbiddenException("Seul le facilitateur peut définir le lien externe.");
        }

        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new DomainValidationException("Le nom du jeu externe ne peut pas être vide.");
        }

        UrlValidation.ValiderHttps(url, "L'URL du jeu externe", requis: true);

        etape.LienExterneNom = nom.Trim();
        etape.LienExterneUrl = url;
        await db.SaveChangesAsync();

        return new DefinirLienExterneResult(etape.LienExterneNom, etape.LienExterneUrl);
    }

    private async Task<Etape> GetEtapeActiveAsync(Guid boardId, Guid etapeId)
    {
        var etape = await db.Etapes.Include(e => e.MiniJeuCatalogue).FirstOrDefaultAsync(e => e.Id == etapeId && e.BoardId == boardId);
        if (etape is null)
        {
            throw new DomainNotFoundException($"Étape {etapeId} introuvable sur ce board.");
        }

        if (etape.Type != TypeEtape.MiniJeu)
        {
            throw new DomainValidationException("Cette étape n'est pas un mini-jeu.");
        }

        if (etape.Statut != StatutEtape.Active)
        {
            throw new DomainValidationException("Cette étape n'est plus active.");
        }

        return etape;
    }

    private async Task<Participant> GetParticipantAsync(Guid boardId, Guid participantId)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.BoardId == boardId);
        if (participant is null)
        {
            throw new DomainNotFoundException($"Participant {participantId} introuvable sur ce board.");
        }

        return participant;
    }
}
