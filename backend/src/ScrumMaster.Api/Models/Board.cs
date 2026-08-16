namespace ScrumMaster.Api.Models;

public enum BoardStatut
{
    Actif,
    Cloture,
}

public class Board
{
    public Guid Id { get; set; }

    public string AreaPath { get; set; } = string.Empty;

    public Equipe? Equipe { get; set; }

    public string Iteration { get; set; } = string.Empty;

    public Guid ThemeId { get; set; }

    public Theme? Theme { get; set; }

    public BoardStatut Statut { get; set; } = BoardStatut.Actif;

    public DateTimeOffset DateCreation { get; set; }

    public int MaxVotesParParticipant { get; set; } = 3;

    public List<Participant> Participants { get; set; } = new();

    public List<PostIt> PostIts { get; set; } = new();
}
