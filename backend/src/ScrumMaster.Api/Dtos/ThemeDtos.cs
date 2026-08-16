namespace ScrumMaster.Api.Dtos;

public record ThemeSummaryDto(Guid Id, string Nom, IReadOnlyList<string> Colonnes);

public record ThemePersonnaliseDto(string Nom, IReadOnlyList<string> Colonnes);
