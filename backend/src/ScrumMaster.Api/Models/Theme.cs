namespace ScrumMaster.Api.Models;

public class Theme
{
    public Guid Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public bool EstPredefini { get; set; }

    /// <summary>Thème appliqué automatiquement si le facilitateur n'en choisit aucun (FR-002).</summary>
    public bool EstParDefaut { get; set; }

    public List<Colonne> Colonnes { get; set; } = new();
}
