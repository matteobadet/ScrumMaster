namespace ScrumMaster.Api.Models;

public class Colonne
{
    public Guid Id { get; set; }

    public Guid ThemeId { get; set; }

    public Theme? Theme { get; set; }

    public string Intitule { get; set; } = string.Empty;

    public int Ordre { get; set; }
}
