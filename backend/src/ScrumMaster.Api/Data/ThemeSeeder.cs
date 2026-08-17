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
            ("Start", null, null, null),
            ("Stop", null, null, null),
            ("Continue", null, null, null)
        );

        await EnsureThemeAsync(
            db,
            "Mad / Sad / Glad",
            estParDefaut: false,
            icone: null,
            contexte: null,
            ("Mad", null, null, null),
            ("Sad", null, null, null),
            ("Glad", null, null, null)
        );

        // Thème entièrement habillé (couleur + illustration + sous-titre sur chaque colonne),
        // démontrable sans configuration manuelle du facilitateur (FR-008, US3, research.md#5).
        // Les illustrations pointent vers `placehold.co` (images déterministes, gratuites, HTTPS)
        // plutôt que de vraies photographies tierces — voir research.md#5 pour la justification.
        // Les sous-titres reprennent les questions directrices de la capture d'écran de référence.
        await EnsureThemeAsync(
            db,
            "La rétro du randonneur",
            estParDefaut: false,
            icone: "🥾",
            contexte: "Une expédition en montagne, comme notre sprint qui vient de s'achever.",
            (
                "La corde",
                "#f5e6b8",
                "https://placehold.co/128/f5e6b8/7a6a2e?text=Corde",
                "Qu'est-ce qui nous a aidé à atteindre notre objectif ?"
            ),
            (
                "Le rocher",
                "#e4e2e8",
                "https://placehold.co/128/e4e2e8/4a4a52?text=Rocher",
                "Quels obstacles nous ont empêchés d'atteindre notre objectif ?"
            ),
            (
                "La météo du voyage",
                "#cfe3f5",
                "https://placehold.co/128/cfe3f5/2b5a80?text=Meteo",
                "Quelles émotions ressentons-nous vis-à-vis de ce périple ?"
            ),
            (
                "Journal de randonnée",
                "#f5ddc8",
                "https://placehold.co/128/f5ddc8/7a4a1f?text=Journal",
                "Qu'avons-nous appris tout au long de notre ascension ?"
            ),
            (
                "Trousse de secours",
                "#f5cfcf",
                "https://placehold.co/128/f5cfcf/802b2b?text=Secours",
                "Qu'est-ce qui rendrait notre prochaine expédition plus facile ?"
            )
        );
    }

    private static async Task EnsureThemeAsync(
        ScrumMasterDbContext db,
        string nom,
        bool estParDefaut,
        string? icone,
        string? contexte,
        params (string Intitule, string? Couleur, string? UrlIllustration, string? SousTitre)[] colonnes
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
                    SousTitre = colonne.SousTitre,
                }
            )
            .ToList();

        db.Themes.Add(theme);
        await db.SaveChangesAsync();
    }
}
