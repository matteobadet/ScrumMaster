namespace ScrumMaster.Api.Models;

/// <summary>
/// Accès Azure DevOps d'une équipe — voir specs/005-azure-devops-boards. Une seule configuration
/// active par équipe ; le PAT est toujours chiffré (jamais stocké ni exposé en clair).
/// </summary>
public class ConfigurationAzureDevOps
{
    public string AreaPath { get; set; } = string.Empty;

    public Equipe? Equipe { get; set; }

    public string Organisation { get; set; } = string.Empty;

    public string Projet { get; set; } = string.Empty;

    public string PatChiffre { get; set; } = string.Empty;

    public DateTimeOffset DateConfiguration { get; set; }
}
