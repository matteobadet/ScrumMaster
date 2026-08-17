namespace ScrumMaster.Api.Models;

/// <summary>Option de réponse d'une étape de poll personnalisé — voir specs/006-systeme-extensions-etapes.</summary>
public class OptionPollPersonnalise
{
    public Guid Id { get; set; }

    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public string Texte { get; set; } = string.Empty;

    public int Ordre { get; set; }
}
