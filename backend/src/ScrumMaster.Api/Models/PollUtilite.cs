namespace ScrumMaster.Api.Models;

public enum TypeReunion
{
    Melee,
    Retrospective,
}

public enum StatutPoll
{
    Ouvert,
    Cloture,
}

public class PollUtilite
{
    public Guid Id { get; set; }

    public string AreaPath { get; set; } = string.Empty;

    public Equipe? Equipe { get; set; }

    public TypeReunion TypeReunion { get; set; }

    public DateOnly Date { get; set; }

    public StatutPoll Statut { get; set; } = StatutPoll.Ouvert;

    public DateTimeOffset DateCreation { get; set; }

    public DateTimeOffset? DateCloture { get; set; }

    public List<VoteUtilite> Votes { get; set; } = new();
}
