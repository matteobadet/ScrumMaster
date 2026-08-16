namespace ScrumMaster.Api.Dtos;

public record ThemeSummaryDto(Guid Id, string Nom, string? Icone, string? Contexte, IReadOnlyList<string> Colonnes);

public record ThemePersonnaliseDto(string Nom, string? Icone, string? Contexte, IReadOnlyList<string> Colonnes);
