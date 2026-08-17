namespace ScrumMaster.Api.AzureDevOps;

// DTOs de désérialisation des réponses brutes de l'API REST Azure DevOps — voir research.md.

public record AzureDevOpsIterationAttributesDto(DateTimeOffset? StartDate, DateTimeOffset? FinishDate);

public record AzureDevOpsIterationNodeDto(string Name, AzureDevOpsIterationAttributesDto? Attributes);

public record AzureDevOpsIterationTreeDto(string Name, List<AzureDevOpsIterationNodeDto>? Children);

public record AzureDevOpsWiqlResultDto(List<AzureDevOpsWorkItemRefDto> WorkItems);

public record AzureDevOpsWorkItemRefDto(int Id);

public record AzureDevOpsWorkItemsBatchDto(List<AzureDevOpsWorkItemDto> Value);

public record AzureDevOpsWorkItemDto(int Id, Dictionary<string, object> Fields);

public record AzureDevOpsCreatedWorkItemDto(int Id);

public record AzureDevOpsWorkItemStateDto(string Name, string Category);

public record AzureDevOpsWorkItemStatesResponseDto(List<AzureDevOpsWorkItemStateDto> Value);

// Résultats exposés par AzureDevOpsClient — indépendants du format brut Azure DevOps.

public record AzureDevOpsIterationSummary(string CheminIteration, bool EnCours);

public record AzureDevOpsWorkItemSummary(int Id, string Titre, string Type, string Etat);

/// <summary>
/// Catégorie normalisée d'un état de work item, indépendante du modèle de processus (Basic,
/// Agile, Scrum, CMMI) — voir specs/009-sprint-review-stats/research.md#1.
/// </summary>
public enum AzureDevOpsEtatCategorie
{
    Proposed,
    InProgress,
    Resolved,
    Completed,
    Removed,
}
