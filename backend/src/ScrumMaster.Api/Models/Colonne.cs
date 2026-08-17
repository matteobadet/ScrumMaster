namespace ScrumMaster.Api.Models;

public class Colonne
{
    public Guid Id { get; set; }

    public Guid ThemeId { get; set; }

    public Theme? Theme { get; set; }

    public string Intitule { get; set; } = string.Empty;

    public int Ordre { get; set; }

    public string? Couleur { get; set; }

    public string? UrlIllustration { get; set; }

    /// <summary>Sous-titre/question directrice de la colonne (ex: "Qu'est-ce qui nous a aidé à atteindre notre objectif ?").</summary>
    public string? SousTitre { get; set; }
}
