using System.Net;
using System.Net.Http.Json;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Habillage visuel par colonne — couleur (US1) et illustration (US2), specs/007-themes-visuels-colonnes.</summary>
public class ThemeVisuelColonneTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ThemeVisuelColonneTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBoard_AvecCouleursDeColonnes_LesRenvoieEtLaissePasVideLesColonnesSansCouleur()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto(
                "Mon thème",
                null,
                null,
                [new ColonneSummaireDto("Start", "#d4f5d4", null), new ColonneSummaireDto("Stop", null, null)]
            ),
            null,
            "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId);
        var colonnes = state.Etapes[0].Colonnes!;

        Assert.Equal("#d4f5d4", colonnes.Single(c => c.Intitule == "Start").Couleur);
        Assert.Null(colonnes.Single(c => c.Intitule == "Stop").Couleur);
    }

    [Fact]
    public async Task CreateBoard_AvecSousTitresDeColonnes_LesRenvoieEtLaissePasVideLesColonnesSans()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto(
                "Mon thème",
                null,
                null,
                [
                    new ColonneSummaireDto("Start", null, null, "Qu'est-ce qu'on continue ?"),
                    new ColonneSummaireDto("Stop", null, null),
                ]
            ),
            null,
            "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId);
        var colonnes = state.Etapes[0].Colonnes!;

        Assert.Equal("Qu'est-ce qu'on continue ?", colonnes.Single(c => c.Intitule == "Start").SousTitre);
        Assert.Null(colonnes.Single(c => c.Intitule == "Stop").SousTitre);
    }

    [Fact]
    public async Task CreateBoard_AvecSousTitreTropLong_Retourne400()
    {
        var sousTitreTropLong = new string('a', 151);
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto("Mon thème", null, null, [new ColonneSummaireDto("Start", null, null, sousTitreTropLong)]),
            null,
            "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_AvecCouleurTropLongue_Retourne400()
    {
        var couleurTropLongue = new string('a', 31);
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto("Mon thème", null, null, [new ColonneSummaireDto("Start", couleurTropLongue, null)]),
            null,
            "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_AvecUrlIllustrationHttpsValide_LaRenvoie()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto(
                "Mon thème",
                null,
                null,
                [new ColonneSummaireDto("Start", null, "https://example.com/start.png"), new ColonneSummaireDto("Stop", null, null)]
            ),
            null,
            "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId);
        var colonnes = state.Etapes[0].Colonnes!;

        Assert.Equal("https://example.com/start.png", colonnes.Single(c => c.Intitule == "Start").UrlIllustration);
        Assert.Null(colonnes.Single(c => c.Intitule == "Stop").UrlIllustration);
    }

    [Fact]
    public async Task CreateBoard_AvecUrlIllustrationNonHttps_Retourne400()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto("Mon thème", null, null, [new ColonneSummaireDto("Start", null, "http://example.com/start.png")]),
            null,
            "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_AvecUrlIllustrationTropLongue_Retourne400()
    {
        var urlTropLongue = "https://example.com/" + new string('a', 2048);
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-138",
            null,
            new ThemePersonnaliseDto("Mon thème", null, null, [new ColonneSummaireDto("Start", null, urlTropLongue)]),
            null,
            "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetThemes_ContientLeThemeRandonneurEntierementHabille()
    {
        var response = await _client.GetAsync("/api/themes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var themes = await response.Content.ReadFromJsonAsync<List<ThemeSummaryDto>>();

        var randonneur = themes!.SingleOrDefault(t => t.Nom == "La rétro du randonneur");
        Assert.NotNull(randonneur);
        Assert.NotEmpty(randonneur!.Colonnes);
        Assert.All(randonneur.Colonnes, c => Assert.NotNull(c.Couleur));
        Assert.All(randonneur.Colonnes, c => Assert.NotNull(c.UrlIllustration));
        Assert.All(randonneur.Colonnes, c => Assert.NotNull(c.SousTitre));
    }

    [Fact]
    public async Task CreateBoard_AvecThemeRandonneur_CopieLesCouleursEtIllustrationsSurLesColonnes()
    {
        var themesResponse = await _client.GetAsync("/api/themes");
        var themes = await themesResponse.Content.ReadFromJsonAsync<List<ThemeSummaryDto>>();
        var randonneur = themes!.Single(t => t.Nom == "La rétro du randonneur");

        var request = new CreateBoardRequest("Krypton", "Sprint-138", randonneur.Id, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId);
        var colonnes = state.Etapes[0].Colonnes!;

        Assert.Equal(randonneur.Colonnes.Count, colonnes.Count);
        Assert.All(colonnes, c => Assert.NotNull(c.Couleur));
        Assert.All(colonnes, c => Assert.NotNull(c.UrlIllustration));
        Assert.All(colonnes, c => Assert.NotNull(c.SousTitre));
    }

    private async Task<BoardStateDto> GetBoardStateAsync(Guid boardId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}");
        return (await response.Content.ReadFromJsonAsync<BoardStateDto>())!;
    }
}
