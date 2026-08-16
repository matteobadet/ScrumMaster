namespace ScrumMaster.Api.Models;

public class PostIt
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board? Board { get; set; }

    public Guid ColonneId { get; set; }

    public Colonne? Colonne { get; set; }

    public string Texte { get; set; } = string.Empty;

    public Guid AuteurParticipantId { get; set; }

    public Participant? Auteur { get; set; }

    public DateTimeOffset DateCreation { get; set; }

    public DateTimeOffset DateModification { get; set; }

    public List<Vote> Votes { get; set; } = new();
}
