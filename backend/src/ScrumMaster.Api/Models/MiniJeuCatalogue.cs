namespace ScrumMaster.Api.Models;

/// <summary>
/// Catalogue de mini-jeux prédéfinis proposés à une étape de type MiniJeu — voir
/// specs/006-systeme-extensions-etapes/research.md#6. Table de données, pas un mécanisme de
/// chargement dynamique : ajouter un mini-jeu nécessite un nouveau composant frontend et une
/// ligne de seed, pas une nouvelle release du moteur d'étapes.
/// </summary>
public class MiniJeuCatalogue
{
    public Guid Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    /// <summary>Clé utilisée par le frontend pour choisir quel composant afficher (ex: "meteo-equipe").</summary>
    public string TypeInterne { get; set; } = string.Empty;

    public string? Description { get; set; }
}
