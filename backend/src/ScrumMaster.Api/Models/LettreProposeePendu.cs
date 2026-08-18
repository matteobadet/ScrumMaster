namespace ScrumMaster.Api.Models;

/// <summary>
/// Une lettre proposée par l'équipe pour le mini-jeu "Pendu" — journal partagé, append-only,
/// distinct du mécanisme "réponse par participant" de Météo/ROTI (specs/011-pendu-lien-externe,
/// research.md#1). La clé primaire (EtapeId, Lettre) garantit l'idempotence par construction
/// (research.md#3).
/// </summary>
public class LettreProposeePendu
{
    public Guid EtapeId { get; set; }

    public Etape? Etape { get; set; }

    public char Lettre { get; set; }

    public bool Correcte { get; set; }

    public Guid ParticipantProposantId { get; set; }

    public Participant? ParticipantProposant { get; set; }

    public DateTimeOffset DateProposition { get; set; }
}
