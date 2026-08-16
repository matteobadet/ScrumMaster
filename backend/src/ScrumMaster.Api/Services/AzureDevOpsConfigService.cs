using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.AzureDevOps;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record ConfigurationAzureDevOpsResult(string AreaPath, string Organisation, string Projet);

/// <summary>
/// Configuration de l'accès Azure DevOps d'une équipe (US1) — voir specs/005-azure-devops-boards.
/// Aucun contrôle de rôle supplémentaire (clarification de spec.md, cohérent avec
/// specs/002-poll-utilite-reunion).
/// </summary>
public class AzureDevOpsConfigService(ScrumMasterDbContext db, AzureDevOpsClient client, IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("ScrumMaster.AzureDevOps.Pat");

    /// <summary>Valide puis enregistre (ou remplace) l'accès Azure DevOps d'une équipe (FR-001, FR-003, FR-004).</summary>
    public async Task<ConfigurationAzureDevOpsResult> ConfigurerAsync(string areaPath, string organisation, string projet, string pat)
    {
        if (string.IsNullOrWhiteSpace(organisation) || string.IsNullOrWhiteSpace(projet) || string.IsNullOrWhiteSpace(pat))
        {
            throw new DomainValidationException("L'organisation, le projet et le PAT sont obligatoires.");
        }

        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.AreaPath == areaPath);
        if (equipe is null)
        {
            throw new DomainNotFoundException(
                $"Équipe \"{areaPath}\" introuvable. Elle doit déjà exister (créée via un board de rétrospective)."
            );
        }

        bool valide;
        try
        {
            valide = await client.ValiderAccesAsync(organisation, projet, pat);
        }
        catch (HttpRequestException)
        {
            valide = false;
        }

        if (!valide)
        {
            // FR-002 : ne jamais inclure le PAT dans un message d'erreur.
            throw new DomainValidationException(
                "Impossible de valider l'accès Azure DevOps : vérifiez l'organisation, le projet et le PAT."
            );
        }

        var configuration = await db.ConfigurationsAzureDevOps.FirstOrDefaultAsync(c => c.AreaPath == areaPath);
        if (configuration is null)
        {
            configuration = new ConfigurationAzureDevOps { AreaPath = areaPath };
            db.ConfigurationsAzureDevOps.Add(configuration);
        }

        configuration.Organisation = organisation;
        configuration.Projet = projet;
        configuration.PatChiffre = _protector.Protect(pat);
        configuration.DateConfiguration = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return new ConfigurationAzureDevOpsResult(areaPath, organisation, projet);
    }
}
