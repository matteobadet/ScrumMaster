namespace ScrumMaster.Api.Models;

public enum NiveauRoti
{
    PerteDeTemps,
    PeuRentable,
    MoyennementRentable,
    Rentable,
    TresRentable,
}

/// <summary>Réponse d'un participant au mini-jeu "ROTI" — voir specs/008-roti-mini-jeu.</summary>
public class ReponseRoti
{
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public Guid ParticipantId { get; set; }

    public Participant? Participant { get; set; }

    public NiveauRoti Niveau { get; set; }

    public DateTimeOffset DateReponse { get; set; }
}
