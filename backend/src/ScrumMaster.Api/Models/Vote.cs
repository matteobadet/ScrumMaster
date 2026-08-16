namespace ScrumMaster.Api.Models;

public class Vote
{
    public Guid PostItId { get; set; }

    public PostIt? PostIt { get; set; }

    public Guid ParticipantId { get; set; }

    public Participant? Participant { get; set; }
}
