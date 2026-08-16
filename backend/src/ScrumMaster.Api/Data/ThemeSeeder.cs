using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Data;

/// <summary>Thèmes prédéfinis proposés à la création d'un board (FR-002, FR-003).</summary>
public static class ThemeSeeder
{
    public static async Task EnsureSeededAsync(ScrumMasterDbContext db)
    {
        if (await db.Themes.AnyAsync())
        {
            return;
        }

        db.Themes.AddRange(
            CreateTheme("Start / Stop / Continue", ["Start", "Stop", "Continue"], estParDefaut: true),
            CreateTheme("Mad / Sad / Glad", ["Mad", "Sad", "Glad"], estParDefaut: false)
        );

        await db.SaveChangesAsync();
    }

    private static Theme CreateTheme(string nom, string[] colonnes, bool estParDefaut)
    {
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            EstPredefini = true,
            EstParDefaut = estParDefaut,
        };

        theme.Colonnes = colonnes
            .Select(
                (intitule, index) =>
                    new Colonne
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = theme.Id,
                        Intitule = intitule,
                        Ordre = index,
                    }
            )
            .ToList();

        return theme;
    }
}
