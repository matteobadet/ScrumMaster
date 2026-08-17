namespace ScrumMaster.Api.Models;

/// <summary>
/// Réponse d'un participant à une étape de poll personnalisé — voir
/// specs/006-systeme-extensions-etapes. Remplaçable tant que l'étape est active (FR-011), même
/// pattern d'upsert que VoteUtilite (specs/002-poll-utilite-reunion).
/// </summary>
public class ReponsePollPersonnalise
{
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public Guid ParticipantId { get; set; }

    public Participant? Participant { get; set; }

    public Guid OptionId { get; set; }

    public OptionPollPersonnalise? Option { get; set; }

    public DateTimeOffset DateReponse { get; set; }
}
