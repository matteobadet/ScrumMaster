namespace ScrumMaster.Api.Models;

public enum ReponseVote
{
    Utile,
    PasNecessaire,
}

public class VoteUtilite
{
    public Guid PollId { get; set; }

    public PollUtilite? Poll { get; set; }

    public string TeamsUserId { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;

    public ReponseVote Reponse { get; set; }

    public DateTimeOffset DateVote { get; set; }
}
