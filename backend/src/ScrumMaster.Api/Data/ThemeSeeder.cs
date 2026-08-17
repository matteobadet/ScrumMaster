using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Data;

/// <summary>Thèmes prédéfinis proposés à la création d'un board (FR-002, FR-003).</summary>
public static class ThemeSeeder
{
    /// <summary>
    /// Idempotent par thème (vérifie l'existence par <see cref="Theme.Nom"/> plutôt que
    /// globalement sur toute la table) : permet d'ajouter un nouveau thème prédéfini à une base
    /// déjà seedée sans réinitialiser les thèmes existants (specs/007-themes-visuels-colonnes).
    /// </summary>
    public static async Task EnsureSeededAsync(ScrumMasterDbContext db)
    {
        await EnsureThemeAsync(
            db,
            "Start / Stop / Continue",
            estParDefaut: true,
            icone: null,
            contexte: null,
            ("Start", null, null),
            ("Stop", null, null),
            ("Continue", null, null)
        );

        await EnsureThemeAsync(
            db,
            "Mad / Sad / Glad",
            estParDefaut: false,
            icone: null,
            contexte: null,
            ("Mad", null, null),
            ("Sad", null, null),
            ("Glad", null, null)
        );

        // Thème entièrement habillé (couleur + illustration sur chaque colonne), démontrable sans
        // configuration manuelle du facilitateur (FR-008, US3, research.md#5). Les illustrations
        // pointent vers `placehold.co` (images déterministes, gratuites, HTTPS) plutôt que de
        // vraies photographies tierces — voir research.md#5 pour la justification.
        await EnsureThemeAsync(
            db,
            "La rétro du randonneur",
            estParDefaut: false,
            icone: "🥾",
            contexte: "Une expédition en montagne, comme notre sprint qui vient de s'achever.",
            ("La corde", "#f5e6b8", "https://placehold.co/128/f5e6b8/7a6a2e?text=Corde"),
            ("Le rocher", "#e4e2e8", "https://placehold.co/128/e4e2e8/4a4a52?text=Rocher"),
            ("La météo du voyage", "#cfe3f5", "https://placehold.co/128/cfe3f5/2b5a80?text=Meteo"),
            ("Journal de randonnée", "#f5ddc8", "https://placehold.co/128/f5ddc8/7a4a1f?text=Journal"),
            ("Trousse de secours", "#f5cfcf", "https://placehold.co/128/f5cfcf/802b2b?text=Secours")
        );
    }

    private static async Task EnsureThemeAsync(
        ScrumMasterDbContext db,
        string nom,
        bool estParDefaut,
        string? icone,
        string? contexte,
        params (string Intitule, string? Couleur, string? UrlIllustration)[] colonnes
    )
    {
        if (await db.Themes.AnyAsync(t => t.Nom == nom))
        {
            return;
        }

        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Icone = icone,
            Contexte = contexte,
            EstPredefini = true,
            EstParDefaut = estParDefaut,
        };

        theme.Colonnes = colonnes
            .Select(
                (colonne, index) => new Colonne
                {
                    Id = Guid.NewGuid(),
                    ThemeId = theme.Id,
                    Intitule = colonne.Intitule,
                    Ordre = index,
                    Couleur = colonne.Couleur,
                    UrlIllustration = colonne.UrlIllustration,
                }
            )
            .ToList();

        db.Themes.Add(theme);
        await db.SaveChangesAsync();
    }
}
