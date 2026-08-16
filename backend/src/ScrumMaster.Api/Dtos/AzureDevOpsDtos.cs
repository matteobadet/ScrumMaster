namespace ScrumMaster.Api.Dtos;

public record AzureDevOpsConfigRequest(string Organisation, string Projet, string Pat);

public record AzureDevOpsConfigResponse(string AreaPath, string Organisation, string Projet);

public record EquipeAzureDevOpsDto(string AreaPath);

public record IterationAzureDevOpsDto(string CheminIteration, bool EnCours);
