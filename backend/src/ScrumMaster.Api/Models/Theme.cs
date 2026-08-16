namespace ScrumMaster.Api.Models;

public class Theme
{
    public Guid Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public bool EstPredefini { get; set; }

    /// <summary>Thème appliqué automatiquement si le facilitateur n'en choisit aucun (FR-002).</summary>
    public bool EstParDefaut { get; set; }

    /// <summary>Icône ou emoji affiché à côté du nom du thème dans l'en-tête du board (specs/004-themes-narratifs).</summary>
    public string? Icone { get; set; }

    /// <summary>Texte libre affiché en introduction du board, avant les colonnes (specs/004-themes-narratifs).</summary>
    public string? Contexte { get; set; }

    public List<Colonne> Colonnes { get; set; } = new();
}
