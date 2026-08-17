namespace ScrumMaster.Api.Dtos;

public record CreateBoardRequest(
    string AreaPath,
    string Iteration,
    Guid? ThemeId,
    ThemePersonnaliseDto? ThemePersonnalise,
    int? MaxVotesParParticipant,
    string NomAffiche,
    IReadOnlyList<EtapeRequestDto>? Etapes = null
);

public record CreateBoardResponse(Guid BoardId, Guid ParticipantId, string Role, string LienAcces);

public record JoinBoardRequest(string NomAffiche);

public record JoinBoardResponse(Guid ParticipantId, string Role);

public record ThemeRefDto(Guid Id, string Nom, string? Icone, string? Contexte);

public record ColonneDto(Guid Id, string Intitule, int Ordre, string? Couleur, string? UrlIllustration, string? SousTitre);

public record PostItDto(
    Guid Id,
    Guid ColonneId,
    string Texte,
    string Auteur,
    Guid AuteurParticipantId,
    int NombreVotes,
    bool VoteDuParticipant,
    int? WorkItemExporteId
);

public record ParticipantDto(Guid Id, string NomAffiche, string Role);

public record ChangeThemeRequest(Guid? ThemeId, ThemePersonnaliseDto? ThemePersonnalise);

public record ChangeThemeResult(Guid ThemeId, string Nom, string? Icone, string? Contexte, IReadOnlyList<ColonneDto> Colonnes);

public record BoardStateDto(
    Guid BoardId,
    string AreaPath,
    string Iteration,
    string Statut,
    int MaxVotesParParticipant,
    IReadOnlyList<EtapeDto> Etapes,
    IReadOnlyList<ParticipantDto> Participants
);
