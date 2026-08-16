using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

/// <summary>
/// Envoi (et dédoublonnage) des rappels de réunion — voir specs/003-rappel-reunion-teams.
/// Séparé de <see cref="PollService"/> : un rappel manuel n'a pas besoin d'un poll
/// (research.md#2).
/// </summary>
public class RappelService(ScrumMasterDbContext db)
{
    /// <summary>
    /// Enregistre le rappel automatique déclenché par la clôture d'un poll "réunion maintenue"
    /// (FR-001), sauf si un rappel a déjà été envoyé aujourd'hui pour cette réunion — dans ce cas
    /// silencieux, sans erreur (FR-008, pas de déclencheur humain à informer).
    /// </summary>
    /// <returns><c>true</c> si le rappel a été enregistré et doit être envoyé, <c>false</c> sinon.</returns>
    public async Task<bool> EnvoyerRappelAutomatiqueSiPossibleAsync(string teamsChannelId, TypeReunion typeReunion)
    {
        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.TeamsChannelId == teamsChannelId);
        if (equipe is null)
        {
            return false;
        }

        return await EnregistrerSiPossibleAsync(equipe.AreaPath, typeReunion);
    }

    /// <summary>Enregistre un rappel manuel (US2), rejeté si le channel n'est pas associé ou si un rappel existe déjà aujourd'hui.</summary>
    public async Task EnvoyerRappelManuelAsync(string teamsChannelId, TypeReunion typeReunion)
    {
        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.TeamsChannelId == teamsChannelId);
        if (equipe is null)
        {
            throw new DomainValidationException(
                "Ce channel n'est associé à aucune équipe. Utilisez d'abord \"associer <area-path>\"."
            );
        }

        var envoye = await EnregistrerSiPossibleAsync(equipe.AreaPath, typeReunion);
        if (!envoye)
        {
            throw new DomainValidationException("Un rappel a déjà été envoyé aujourd'hui pour cette réunion.");
        }
    }

    private async Task<bool> EnregistrerSiPossibleAsync(string areaPath, TypeReunion typeReunion)
    {
        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var dejaEnvoye = await db.RappelsEnvoyes.AnyAsync(r =>
            r.AreaPath == areaPath && r.TypeReunion == typeReunion && r.Date == aujourdhui
        );
        if (dejaEnvoye)
        {
            return false;
        }

        db.RappelsEnvoyes.Add(
            new RappelEnvoye
            {
                Id = Guid.NewGuid(),
                AreaPath = areaPath,
                TypeReunion = typeReunion,
                Date = aujourdhui,
                DateEnvoi = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();
        return true;
    }
}
