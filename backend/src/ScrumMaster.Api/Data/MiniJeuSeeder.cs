using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Data;

/// <summary>Mini-jeux prédéfinis proposés pour une étape de type MiniJeu (specs/006-systeme-extensions-etapes).</summary>
public static class MiniJeuSeeder
{
    public static async Task EnsureSeededAsync(ScrumMasterDbContext db)
    {
        if (await db.MiniJeuxCatalogue.AnyAsync())
        {
            return;
        }

        db.MiniJeuxCatalogue.Add(
            new MiniJeuCatalogue
            {
                Id = Guid.NewGuid(),
                Nom = "Météo d'équipe",
                TypeInterne = "meteo-equipe",
                Description = "Chaque participant choisit l'humeur qui reflète son état d'esprit du moment.",
            }
        );

        await db.SaveChangesAsync();
    }
}
