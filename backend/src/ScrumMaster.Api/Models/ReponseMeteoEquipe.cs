namespace ScrumMaster.Api.Models;

public enum HumeurMeteo
{
    Ensoleille,
    Nuageux,
    Pluvieux,
    Orageux,
}

/// <summary>Réponse d'un participant au mini-jeu "Météo d'équipe" — voir specs/006-systeme-extensions-etapes.</summary>
public class ReponseMeteoEquipe
{
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public Guid ParticipantId { get; set; }

    public Participant? Participant { get; set; }

    public HumeurMeteo Humeur { get; set; }

    public DateTimeOffset DateReponse { get; set; }
}
