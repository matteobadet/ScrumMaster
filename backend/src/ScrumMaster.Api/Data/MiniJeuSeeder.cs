using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Data;

/// <summary>Mini-jeux prédéfinis proposés pour une étape de type MiniJeu (specs/006-systeme-extensions-etapes).</summary>
public static class MiniJeuSeeder
{
    /// <summary>
    /// Idempotent par mini-jeu (vérifie l'existence par <see cref="MiniJeuCatalogue.TypeInterne"/>
    /// plutôt que globalement sur toute la table) : permet d'ajouter un nouveau mini-jeu à une
    /// base déjà seedée sans réinitialiser les mini-jeux existants (même correctif que
    /// `ThemeSeeder`, specs/007-themes-visuels-colonnes ; specs/008-roti-mini-jeu, research.md#5).
    /// </summary>
    public static async Task EnsureSeededAsync(ScrumMasterDbContext db)
    {
        await EnsureMiniJeuAsync(
            db,
            nom: "Météo d'équipe",
            typeInterne: "meteo-equipe",
            description: "Chaque participant choisit l'humeur qui reflète son état d'esprit du moment."
        );

        await EnsureMiniJeuAsync(
            db,
            nom: "ROTI",
            typeInterne: "roti",
            description: "Chaque participant évalue si le temps investi dans la rétrospective en valait la peine."
        );

        await EnsureMiniJeuAsync(
            db,
            nom: "Pendu",
            typeInterne: "pendu",
            description: "L'équipe devine collectivement un mot choisi par le facilitateur, lettre par lettre."
        );

        await EnsureMiniJeuAsync(
            db,
            nom: "Lien externe",
            typeInterne: "lien-externe",
            description: "Le facilitateur redirige l'équipe vers un outil de jeu en ligne externe (Gartic Phone, Skribbl.io...)."
        );
    }

    private static async Task EnsureMiniJeuAsync(ScrumMasterDbContext db, string nom, string typeInterne, string description)
    {
        if (await db.MiniJeuxCatalogue.AnyAsync(m => m.TypeInterne == typeInterne))
        {
            return;
        }

        db.MiniJeuxCatalogue.Add(
            new MiniJeuCatalogue
            {
                Id = Guid.NewGuid(),
                Nom = nom,
                TypeInterne = typeInterne,
                Description = description,
            }
        );

        await db.SaveChangesAsync();
    }
}
