namespace ScrumMaster.Api.Models;

public class PostIt
{
    public Guid Id { get; set; }

    /// <summary>Étape "Colonnes et post-its" qui porte ce post-it (renommé depuis BoardId, specs/006-systeme-extensions-etapes).</summary>
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public Guid ColonneId { get; set; }

    public Colonne? Colonne { get; set; }

    public string Texte { get; set; } = string.Empty;

    public Guid AuteurParticipantId { get; set; }

    public Participant? Auteur { get; set; }

    public DateTimeOffset DateCreation { get; set; }

    public DateTimeOffset DateModification { get; set; }

    /// <summary>Id du work item Azure DevOps d'origine si ce post-it a été importé (specs/005-azure-devops-boards).</summary>
    public int? WorkItemSourceId { get; set; }

    /// <summary>Id du work item Azure DevOps créé si ce post-it a été exporté (specs/005-azure-devops-boards).</summary>
    public int? WorkItemExporteId { get; set; }

    public List<Vote> Votes { get; set; } = new();
}
