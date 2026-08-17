namespace ScrumMaster.Api.Dtos;

/// <summary>Une colonne au sein d'un thème, avec son habillage visuel facultatif (specs/007-themes-visuels-colonnes).</summary>
public record ColonneSummaireDto(string Intitule, string? Couleur, string? UrlIllustration, string? SousTitre = null);

public record ThemeSummaryDto(Guid Id, string Nom, string? Icone, string? Contexte, IReadOnlyList<ColonneSummaireDto> Colonnes);

public record ThemePersonnaliseDto(string Nom, string? Icone, string? Contexte, IReadOnlyList<ColonneSummaireDto> Colonnes);
