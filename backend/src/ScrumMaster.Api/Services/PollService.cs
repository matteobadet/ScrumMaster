using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;

namespace ScrumMaster.Api.Services;

/// <summary>
/// Association channel/équipe, déclenchement, vote et clôture des polls d'utilité — voir
/// specs/002-poll-utilite-reunion. Implémenté progressivement par les User Stories 1 à 3.
/// </summary>
public class PollService(ScrumMasterDbContext db)
{
    /// <summary>
    /// Associe le channel Teams courant à l'équipe (FR-001, FR-002). Aucun contrôle de rôle
    /// n'est appliqué ici : contrairement au facilitateur de board (specs/001-retro-board-base,
    /// identité par session de board), il n'existe pas de mapping durable entre une identité
    /// Teams et un rôle d'équipe dans cette feature — seule l'appartenance au channel Teams
    /// (contrôlée par Teams lui-même) restreint qui peut exécuter cette commande.
    /// </summary>
    public async Task AssocierChannelAsync(string areaPath, string teamsChannelId)
    {
        if (string.IsNullOrWhiteSpace(areaPath))
        {
            throw new DomainValidationException("L'Area Path est obligatoire.");
        }

        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.AreaPath == areaPath);
        if (equipe is null)
        {
            throw new DomainNotFoundException(
                $"Équipe \"{areaPath}\" introuvable. Elle doit déjà exister (créée via un board de rétrospective)."
            );
        }

        equipe.TeamsChannelId = teamsChannelId;
        await db.SaveChangesAsync();
    }
}
