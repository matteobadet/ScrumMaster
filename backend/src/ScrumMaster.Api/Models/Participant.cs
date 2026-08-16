namespace ScrumMaster.Api.Models;

public enum ParticipantRole
{
    Facilitateur,
    Participant,
}

public class Participant
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Board? Board { get; set; }

    public string NomAffiche { get; set; } = string.Empty;

    public ParticipantRole Role { get; set; }

    /// <summary>Connexion SignalR active la plus récente (null si déconnecté) — non un identifiant durable.</summary>
    public string? ConnectionId { get; set; }
}
