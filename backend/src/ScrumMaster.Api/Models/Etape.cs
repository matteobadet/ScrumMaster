namespace ScrumMaster.Api.Models;

public enum TypeEtape
{
    ColonnesEtPostIts,
    MiniJeu,
    PollPersonnalise,
}

public enum StatutEtape
{
    AVenir,
    Active,
    Terminee,
}

/// <summary>
/// Une étape de la séquence d'un board de rétrospective — voir specs/006-systeme-extensions-etapes.
/// Union étiquetée simple (colonnes nullable par type) plutôt qu'une hiérarchie polymorphe : le
/// catalogue de types est fixe et fermé (research.md#1).
/// </summary>
public class Etape
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board? Board { get; set; }

    public TypeEtape Type { get; set; }

    public int Ordre { get; set; }

    public StatutEtape Statut { get; set; } = StatutEtape.AVenir;

    // Colonnes et post-its (Type == ColonnesEtPostIts)
    public Guid? ThemeId { get; set; }

    public Theme? Theme { get; set; }

    public List<PostIt> PostIts { get; set; } = new();

    // Mini-jeu (Type == MiniJeu)
    public Guid? MiniJeuCatalogueId { get; set; }

    public MiniJeuCatalogue? MiniJeuCatalogue { get; set; }

    public List<ReponseMeteoEquipe> ReponsesMeteo { get; set; } = new();

    // Poll personnalisé (Type == PollPersonnalise)
    public string? Question { get; set; }

    public List<OptionPollPersonnalise> Options { get; set; } = new();

    public List<ReponsePollPersonnalise> Reponses { get; set; } = new();
}
