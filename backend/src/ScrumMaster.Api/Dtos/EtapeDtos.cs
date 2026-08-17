namespace ScrumMaster.Api.Dtos;

/// <summary>Une personnalisation de visuel de niveau ROTI (specs/008-roti-mini-jeu).</summary>
public record NiveauVisuelDto(string Niveau, string UrlIllustration);

/// <summary>Une étape demandée à la composition d'une séquence (US1, specs/006-systeme-extensions-etapes).</summary>
public record EtapeRequestDto(
    string Type,
    Guid? ThemeId,
    ThemePersonnaliseDto? ThemePersonnalise,
    Guid? MiniJeuCatalogueId,
    string? Question,
    IReadOnlyList<string>? Options,
    IReadOnlyList<NiveauVisuelDto>? RotiPersonnalisations = null
);

public record MiniJeuRefDto(Guid Id, string Nom, string TypeInterne);

public record ReponseMeteoDto(Guid ParticipantId, string NomAffiche, string Humeur);

public record ReponseRotiDto(Guid ParticipantId, string NomAffiche, string Niveau);

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
    Guid? MaReponseOptionId,
    IReadOnlyList<ReponseRotiDto>? ReponsesRoti = null,
    string? MonNiveauRoti = null,
    IReadOnlyList<NiveauVisuelDto>? VisuelsRoti = null
);
