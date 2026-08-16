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

// Résultats exposés par AzureDevOpsClient — indépendants du format brut Azure DevOps.

public record AzureDevOpsIterationSummary(string CheminIteration, bool EnCours);

public record AzureDevOpsWorkItemSummary(int Id, string Titre);
