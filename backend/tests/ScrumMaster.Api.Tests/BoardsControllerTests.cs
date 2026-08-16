using System.Net;
using System.Net.Http.Json;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class BoardsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BoardsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBoard_SansThemeChoisi_AppliqueLeThemeParDefautEtCreeLeFacilitateur()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: null,
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();
        Assert.NotNull(created);
        Assert.Equal("Facilitateur", created!.Role);

        var stateResponse = await _client.GetAsync($"/api/boards/{created.BoardId}");
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        Assert.NotNull(state);
        Assert.Equal("Krypton", state!.AreaPath);
        Assert.Equal("Sprint-138", state.Iteration);
        Assert.Equal(3, state.Colonnes.Count);
        Assert.Contains(state.Colonnes, c => c.Intitule == "Start");
        Assert.Single(state.Participants);
        Assert.Equal("Facilitateur", state.Participants[0].Role);
    }

    [Fact]
    public async Task CreateBoard_AvecThemePersonnaliseSansColonne_Retourne400()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: new ThemePersonnaliseDto("Vide", []),
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_SansAreaPath_Retourne400()
    {
        var request = new CreateBoardRequest(
            AreaPath: "",
            Iteration: "Sprint-138",
            ThemeId: null,
            ThemePersonnalise: null,
            MaxVotesParParticipant: null,
            NomAffiche: "Alex"
        );

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
