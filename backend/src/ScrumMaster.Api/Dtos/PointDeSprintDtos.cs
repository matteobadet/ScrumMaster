namespace ScrumMaster.Api.Dtos;

/// <summary>Répartition par état pour un type de work item (specs/009-sprint-review-stats).</summary>
public record RepartitionTypeDto(string Type, int AFaire, int EnCours, int Termine);

/// <summary>
/// Statistiques calculées à la demande depuis Azure DevOps pour l'Iteration d'un board — n'est pas
/// une donnée persistée (specs/009-sprint-review-stats, data-model.md).
/// </summary>
public record PointDeSprintDto(string Iteration, IReadOnlyList<RepartitionTypeDto> RepartitionParType, int TotalPlanifie, int TotalTermine);
