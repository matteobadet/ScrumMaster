using System.Net;
using System.Net.Http.Json;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class ThemeIconeContexteTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ThemeIconeContexteTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBoard_AvecThemePersonnaliseIcone_RenvoieLIconeSurLeBoard()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto("Les 3 petits cochons", "🐷", null, ["Paille", "Bois", "Briques"]),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await (await _client.GetAsync($"/api/boards/{created!.BoardId}")).Content.ReadFromJsonAsync<BoardStateDto>();

        Assert.Equal("🐷", state!.Etapes[0].Theme!.Icone);
        Assert.Null(state.Etapes[0].Theme!.Contexte);
    }

    [Fact]
    public async Task CreateBoard_AvecThemePersonnaliseContexte_RenvoieLeContexteSurLeBoard()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto(
                "Les 3 petits cochons",
                null,
                "Qu'est-ce qui a tenu solide ce sprint ?",
                ["Paille", "Bois", "Briques"]
            ),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await (await _client.GetAsync($"/api/boards/{created!.BoardId}")).Content.ReadFromJsonAsync<BoardStateDto>();

        Assert.Equal("Qu'est-ce qui a tenu solide ce sprint ?", state!.Etapes[0].Theme!.Contexte);
        Assert.Null(state.Etapes[0].Theme!.Icone);
    }

    [Fact]
    public async Task CreateBoard_SansIconeNiContexte_NeRenvoieAucuneErreur()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto("Neutre", null, null, ["A", "B"]),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await (await _client.GetAsync($"/api/boards/{created!.BoardId}")).Content.ReadFromJsonAsync<BoardStateDto>();

        Assert.Null(state!.Etapes[0].Theme!.Icone);
        Assert.Null(state.Etapes[0].Theme!.Contexte);
    }

    [Fact]
    public async Task CreateBoard_AvecContexteTropLong_Retourne400()
    {
        var contexteTropLong = new string('a', 501);
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto("Trop long", null, contexteTropLong, ["A"]),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_AvecIconeTropLongue_Retourne400()
    {
        var iconeTropLongue = new string('a', 51);
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto("Trop long", iconeTropLongue, null, ["A"]),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
