namespace ScrumMaster.Api.Dtos;

/// <summary>Une étape demandée à la composition d'une séquence (US1, specs/006-systeme-extensions-etapes).</summary>
public record EtapeRequestDto(
    string Type,
    Guid? ThemeId,
    ThemePersonnaliseDto? ThemePersonnalise,
    Guid? MiniJeuCatalogueId,
    string? Question,
    IReadOnlyList<string>? Options
);

public record MiniJeuRefDto(Guid Id, string Nom, string TypeInterne);

public record ReponseMeteoDto(Guid ParticipantId, string NomAffiche, string Humeur);

public record OptionPollDto(Guid Id, string Texte, int Decompte);

/// <summary>État complet d'une étape, quel que soit son type — les champs non pertinents pour le
/// type de l'étape sont null (union étiquetée, research.md#1).</summary>
public record EtapeDto(
    Guid Id,
    string Type,
    int Ordre,
    string Statut,
    ThemeRefDto? Theme,
    IReadOnlyList<ColonneDto>? Colonnes,
    IReadOnlyList<PostItDto>? PostIts,
    int? MesVotesRestants,
    MiniJeuRefDto? MiniJeu,
    IReadOnlyList<ReponseMeteoDto>? ReponsesMeteo,
    string? MonHumeur,
    string? Question,
    IReadOnlyList<OptionPollDto>? Options,
    Guid? MaReponseOptionId
);
