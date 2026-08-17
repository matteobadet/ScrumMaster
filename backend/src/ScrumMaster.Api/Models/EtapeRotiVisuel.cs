namespace ScrumMaster.Api.Models;

/// <summary>
/// Personnalisation facultative et sparse du visuel d'un niveau ROTI pour une étape précise —
/// une ligne uniquement pour les niveaux personnalisés (research.md#3, specs/008-roti-mini-jeu).
/// </summary>
public class EtapeRotiVisuel
{
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public NiveauRoti Niveau { get; set; }

    public string UrlIllustration { get; set; } = string.Empty;
}
