using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Étape de type Mini-jeu "Lien externe" (specs/011-pendu-lien-externe).</summary>
public class LienExterneTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LienExterneTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record LienExterneDefiniEnvelope(Guid EtapeId, string Nom, string Url);

    [Fact]
    public async Task EtapeLienExterne_SansLienRenseigne_RenvoieUnEtatDAttente()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeLienExterneAsync();

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);

        Assert.Null(etape.LienExterneNom);
        Assert.Null(etape.LienExterneUrl);
    }

    [Fact]
    public async Task DefinirLienExterne_ParLeFacilitateur_EstDiffuseATous()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeLienExterneAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<LienExterneDefiniEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<LienExterneDefiniEnvelope>("LienExterneDefini", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("DefinirLienExterne", boardId, etapeId, "Gartic Phone", "https://garticphone.com/salon-123");
        var evt = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Gartic Phone", evt.Nom);
        Assert.Equal("https://garticphone.com/salon-123", evt.Url);

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("Gartic Phone", etape.LienExterneNom);
        Assert.Equal("https://garticphone.com/salon-123", etape.LienExterneUrl);
    }

    [Fact]
    public async Task DefinirLienExterne_AvecUrlNonHttps_EstRefuse()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeLienExterneAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("DefinirLienExterne", boardId, etapeId, "Skribbl", "http://skribbl.io/insecure")
        );
        Assert.Contains("HTTPS", ex.Message);
    }

    [Fact]
    public async Task DefinirLienExterne_ParUnNonFacilitateur_EstRefuse()
    {
        var (boardId, etapeId, _) = await CreerBoardAvecEtapeLienExterneAsync();
        var participantId = await JoinBoardAsync(boardId, "Sam");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("DefinirLienExterne", boardId, etapeId, "Gartic Phone", "https://garticphone.com/salon-123")
        );
        Assert.Contains("facilitateur", ex.Message);
    }

    [Fact]
    public async Task DefinirLienExterne_UneSecondeFois_RemplaceLePrecedent()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeLienExterneAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("DefinirLienExterne", boardId, etapeId, "Gartic Phone", "https://garticphone.com/salon-123");
        await connection.InvokeAsync("DefinirLienExterne", boardId, etapeId, "Skribbl.io", "https://skribbl.io/salon-456");

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("Skribbl.io", etape.LienExterneNom);
        Assert.Equal("https://skribbl.io/salon-456", etape.LienExterneUrl);
    }

    private async Task<(Guid BoardId, Guid EtapeId, Guid FacilitateurId)> CreerBoardAvecEtapeLienExterneAsync()
    {
        var lienExterneId = await ObtenirMiniJeuIdAsync("lien-externe");
        IReadOnlyList<EtapeRequestDto> etapes = [new EtapeRequestDto("MiniJeu", null, null, lienExterneId, null, null)];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId, created.ParticipantId);
        return (created.BoardId, state.Etapes[0].Id, created.ParticipantId);
    }

    private async Task<Guid> JoinBoardAsync(Guid boardId, string nomAffiche)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/participants", new JoinBoardRequest(nomAffiche));
        var joined = await response.Content.ReadFromJsonAsync<JoinBoardResponse>();
        return joined!.ParticipantId;
    }

    private async Task<Guid> ObtenirMiniJeuIdAsync(string typeInterne)
    {
        var response = await _client.GetAsync("/api/mini-jeux");
        var miniJeux = await response.Content.ReadFromJsonAsync<List<MiniJeuRefDto>>();
        return miniJeux!.Single(m => m.TypeInterne == typeInterne).Id;
    }

    private async Task<BoardStateDto> GetBoardStateAsync(Guid boardId, Guid participantId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}?asParticipantId={participantId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BoardStateDto>())!;
    }

    private HubConnection CreateConnection() =>
        new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/retro-board",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                }
            )
            .Build();
}
