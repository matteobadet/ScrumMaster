using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.AzureDevOps;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record EquipeAzureDevOpsResult(string AreaPath);

public record IterationAzureDevOpsResult(string CheminIteration, bool EnCours);

public record PostItImporteResult(Guid Id, Guid ColonneId, string Texte, string Auteur, Guid AuteurParticipantId);

public record PostItExporteResult(Guid PostItId, int WorkItemId);

public record RepartitionTypeResult(string Type, int AFaire, int EnCours, int Termine);

public record PointDeSprintResult(string Iteration, IReadOnlyList<RepartitionTypeResult> RepartitionParType, int TotalPlanifie, int TotalTermine);

/// <summary>
/// Sélection guidée de l'Area Path/Iteration (US2), import de work items (US3) et export de
/// post-its (US4) — voir specs/005-azure-devops-boards.
/// </summary>
public class AzureDevOpsBoardService(ScrumMasterDbContext db, AzureDevOpsClient client, IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("ScrumMaster.AzureDevOps.Pat");

    /// <summary>Équipes déjà configurées, pour la sélection guidée de l'Area Path (research.md#3).</summary>
    public async Task<IReadOnlyList<EquipeAzureDevOpsResult>> ListerEquipesConfigureesAsync() =>
        (await db.ConfigurationsAzureDevOps.Select(c => c.AreaPath).ToListAsync()).Select(a => new EquipeAzureDevOpsResult(a)).ToList();

    /// <summary>Iterations réelles de l'équipe, avec l'Iteration en cours indiquée (FR-005, FR-005a).</summary>
    public async Task<IReadOnlyList<IterationAzureDevOpsResult>> ObtenirIterationsAsync(string areaPath)
    {
        var configuration = await ObtenirConfigurationAsync(areaPath);

        try
        {
            var iterations = await client.ListerIterationsAsync(configuration.Organisation, configuration.Projet, Dechiffrer(configuration));
            return iterations.Select(i => new IterationAzureDevOpsResult(i.CheminIteration, i.EnCours)).ToList();
        }
        catch (HttpRequestException)
        {
            throw new DomainUpstreamException("Impossible de récupérer les Iterations depuis Azure DevOps pour le moment.");
        }
    }

    /// <summary>Facilitateur uniquement (FR-011) ; un post-it par work item non déjà importé (FR-008).</summary>
    public async Task<IReadOnlyList<PostItImporteResult>> ImporterWorkItemsAsync(Guid boardId, Guid callerParticipantId)
    {
        var board = await ObtenirBoardPourFacilitateurAsync(boardId, callerParticipantId, "importer des work items");
        var etapeActive = ObtenirEtapeActiveColonnesEtPostIts(board);
        var configuration = await db.ConfigurationsAzureDevOps.FirstOrDefaultAsync(c => c.AreaPath == board.AreaPath);
        if (configuration is null)
        {
            throw new DomainValidationException("Cette équipe n'a pas d'accès Azure DevOps configuré.");
        }

        IReadOnlyList<AzureDevOpsWorkItemSummary> workItems;
        try
        {
            workItems = await client.ListerWorkItemsAsync(configuration.Organisation, configuration.Projet, Dechiffrer(configuration), board.Iteration);
        }
        catch (HttpRequestException)
        {
            throw new DomainUpstreamException("Impossible de récupérer les work items depuis Azure DevOps pour le moment.");
        }

        var dejaImportes = await db
            .PostIts.Where(p => p.EtapeId == etapeActive.Id && p.WorkItemSourceId != null)
            .Select(p => p.WorkItemSourceId!.Value)
            .ToListAsync();

        var aImporter = workItems.Where(w => !dejaImportes.Contains(w.Id)).ToList();
        if (aImporter.Count == 0)
        {
            return [];
        }

        var premiereColonne = await db.Colonnes.Where(c => c.ThemeId == etapeActive.ThemeId).OrderBy(c => c.Ordre).FirstAsync();
        var facilitateur = await db.Participants.FirstAsync(p => p.Id == callerParticipantId);
        var maintenant = DateTimeOffset.UtcNow;

        var nouveauxPostIts = aImporter
            .Select(w => new PostIt
            {
                Id = Guid.NewGuid(),
                EtapeId = etapeActive.Id,
                ColonneId = premiereColonne.Id,
                Texte = w.Titre,
                AuteurParticipantId = callerParticipantId,
                DateCreation = maintenant,
                DateModification = maintenant,
                WorkItemSourceId = w.Id,
            })
            .ToList();

        db.PostIts.AddRange(nouveauxPostIts);
        await db.SaveChangesAsync();

        return nouveauxPostIts.Select(p => new PostItImporteResult(p.Id, p.ColonneId, p.Texte, facilitateur.NomAffiche, p.AuteurParticipantId)).ToList();
    }

    /// <summary>Facilitateur uniquement (FR-011) ; refuse un second export du même post-it (FR-010).</summary>
    public async Task<PostItExporteResult> ExporterPostItAsync(Guid boardId, Guid callerParticipantId, Guid postItId)
    {
        var board = await ObtenirBoardPourFacilitateurAsync(boardId, callerParticipantId, "exporter un post-it");
        var etapeActive = ObtenirEtapeActiveColonnesEtPostIts(board);
        var configuration = await db.ConfigurationsAzureDevOps.FirstOrDefaultAsync(c => c.AreaPath == board.AreaPath);
        if (configuration is null)
        {
            throw new DomainValidationException("Cette équipe n'a pas d'accès Azure DevOps configuré.");
        }

        var postIt = await db.PostIts.FirstOrDefaultAsync(p => p.Id == postItId && p.EtapeId == etapeActive.Id);
        if (postIt is null)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        if (postIt.WorkItemExporteId is not null)
        {
            throw new DomainValidationException("Ce post-it a déjà été exporté vers Azure DevOps.");
        }

        int workItemId;
        try
        {
            workItemId = await client.CreerWorkItemAsync(configuration.Organisation, configuration.Projet, Dechiffrer(configuration), postIt.Texte);
        }
        catch (HttpRequestException)
        {
            throw new DomainUpstreamException("Impossible de créer le work item dans Azure DevOps pour le moment.");
        }

        postIt.WorkItemExporteId = workItemId;
        await db.SaveChangesAsync();

        return new PostItExporteResult(postIt.Id, workItemId);
    }

    /// <summary>
    /// Statistiques de l'Iteration du board, calculées à la demande (specs/009-sprint-review-stats).
    /// Ouvert à tout participant, sans exiger que le board soit encore actif (FR-001, research.md#5).
    /// </summary>
    public async Task<PointDeSprintResult> ObtenirPointDeSprintAsync(Guid boardId, Guid callerParticipantId)
    {
        var board = await ObtenirBoardPourParticipantAsync(boardId, callerParticipantId);
        var configuration = await db.ConfigurationsAzureDevOps.FirstOrDefaultAsync(c => c.AreaPath == board.AreaPath);
        if (configuration is null)
        {
            throw new DomainValidationException("Cette équipe n'a pas d'accès Azure DevOps configuré.");
        }

        var pat = Dechiffrer(configuration);

        IReadOnlyList<AzureDevOpsWorkItemSummary> workItems;
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, AzureDevOpsEtatCategorie>> etatsParType;
        try
        {
            workItems = await client.ListerWorkItemsAsync(configuration.Organisation, configuration.Projet, pat, board.Iteration);

            var etats = new Dictionary<string, IReadOnlyDictionary<string, AzureDevOpsEtatCategorie>>();
            foreach (var type in workItems.Select(w => w.Type).Distinct())
            {
                etats[type] = await client.ObtenirEtatsAsync(configuration.Organisation, configuration.Projet, pat, type);
            }

            etatsParType = etats;
        }
        catch (HttpRequestException)
        {
            throw new DomainUpstreamException("Impossible de récupérer les statistiques depuis Azure DevOps pour le moment.");
        }

        var classifies = workItems
            .Select(w => new
            {
                Bucket = TypeBucket(w.Type),
                Categorie = etatsParType.TryGetValue(w.Type, out var etats) && etats.TryGetValue(w.Etat, out var categorie) ? categorie : (AzureDevOpsEtatCategorie?)null,
            })
            .Where(w => w.Categorie is { } categorie && categorie != AzureDevOpsEtatCategorie.Removed)
            .ToList();

        var repartition = classifies
            .GroupBy(w => w.Bucket)
            .Select(g => new RepartitionTypeResult(
                g.Key,
                g.Count(w => w.Categorie == AzureDevOpsEtatCategorie.Proposed),
                g.Count(w => w.Categorie is AzureDevOpsEtatCategorie.InProgress or AzureDevOpsEtatCategorie.Resolved),
                g.Count(w => w.Categorie == AzureDevOpsEtatCategorie.Completed)
            ))
            .ToList();

        return new PointDeSprintResult(
            board.Iteration,
            repartition,
            classifies.Count,
            classifies.Count(w => w.Categorie == AzureDevOpsEtatCategorie.Completed)
        );
    }

    private static string TypeBucket(string type) =>
        type switch
        {
            "Task" => "Task",
            "User Story" => "UserStory",
            _ => "Autres",
        };

    /// <summary>Résout le board pour tout participant (facilitateur ou non), sans exiger qu'il soit
    /// encore actif — pour les consultations en lecture seule (specs/009-sprint-review-stats).</summary>
    private async Task<Board> ObtenirBoardPourParticipantAsync(Guid boardId, Guid callerParticipantId)
    {
        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var caller = await db.Participants.FirstOrDefaultAsync(p => p.Id == callerParticipantId && p.BoardId == boardId);
        if (caller is null)
        {
            throw new DomainNotFoundException($"Participant {callerParticipantId} introuvable sur ce board.");
        }

        return board;
    }

    private async Task<ConfigurationAzureDevOps> ObtenirConfigurationAsync(string areaPath)
    {
        var configuration = await db.ConfigurationsAzureDevOps.FirstOrDefaultAsync(c => c.AreaPath == areaPath);
        if (configuration is null)
        {
            throw new DomainNotFoundException($"Équipe \"{areaPath}\" sans accès Azure DevOps configuré.");
        }

        return configuration;
    }

    private async Task<Board> ObtenirBoardPourFacilitateurAsync(Guid boardId, Guid callerParticipantId, string action)
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
            throw new DomainForbiddenException($"Seul le facilitateur peut {action}.");
        }

        BoardClosureGuard.EnsureActif(board);

        return board;
    }

    /// <summary>Résout l'étape "Colonnes et post-its" active du board (specs/006-systeme-extensions-etapes, research.md#5).</summary>
    private static Etape ObtenirEtapeActiveColonnesEtPostIts(Board board)
    {
        var active = board.Etapes.FirstOrDefault(e => e.Statut == StatutEtape.Active);
        if (active is null || active.Type != TypeEtape.ColonnesEtPostIts)
        {
            throw new DomainValidationException("L'étape active de ce board n'accepte pas l'import/export Azure DevOps.");
        }

        return active;
    }

    private string Dechiffrer(ConfigurationAzureDevOps configuration) => _protector.Unprotect(configuration.PatChiffre);
}
