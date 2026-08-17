using System.Net;
using System.Net.Http.Json;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Historique des boards d'une équipe (specs/010-historique-boards).</summary>
public class HistoriqueBoardsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HistoriqueBoardsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListerBoardsParEquipe_RenvoieTousLesBoardsTriesDuPlusRecentAuPlusAncien()
    {
        var areaPath = $"HistoriqueTest-{Guid.NewGuid():N}";

        var premier = await CreerBoardAsync(areaPath, "Sprint-1");
        await Task.Delay(10);
        var second = await CreerBoardAsync(areaPath, "Sprint-2");

        var response = await _client.GetAsync($"/api/equipes/{areaPath}/boards");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var boards = await response.Content.ReadFromJsonAsync<List<BoardSummaireDto>>();

        Assert.Equal(2, boards!.Count);
        Assert.Equal(second, boards[0].Id);
        Assert.Equal("Sprint-2", boards[0].Iteration);
        Assert.Equal("Actif", boards[0].Statut);
        Assert.Equal(premier, boards[1].Id);
        Assert.Equal("Sprint-1", boards[1].Iteration);
    }

    [Fact]
    public async Task ListerBoardsParEquipe_SansBoard_RenvoieUneListeVide()
    {
        var response = await _client.GetAsync($"/api/equipes/EquipeInconnue-{Guid.NewGuid():N}/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var boards = await response.Content.ReadFromJsonAsync<List<BoardSummaireDto>>();
        Assert.Empty(boards!);
    }

    private async Task<Guid> CreerBoardAsync(string areaPath, string iteration)
    {
        var request = new CreateBoardRequest(areaPath, iteration, null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();
        return created!.BoardId;
    }
}
