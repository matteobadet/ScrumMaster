using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Étape de type Mini-jeu ("Météo d'équipe", US2, specs/006-systeme-extensions-etapes).</summary>
public class MiniJeuTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MiniJeuTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record ReponseMiniJeuChangeeEnvelope(Guid EtapeId, Guid ParticipantId, string NomAffiche, string Reponse);

    [Fact]
    public async Task RepondreMiniJeu_EnregistreLaReponseEtLaDiffuse()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeMiniJeuAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<ReponseMiniJeuChangeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<ReponseMiniJeuChangeeEnvelope>("ReponseMiniJeuChangee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "Ensoleille");
        var reponse = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(facilitateurId, reponse.ParticipantId);
        Assert.Equal("Ensoleille", reponse.Reponse);

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("Ensoleille", etape.MonHumeur);
        Assert.Single(etape.ReponsesMeteo!);
    }

    [Fact]
    public async Task RepondreMiniJeu_UneSecondeFois_RemplaceLaReponsePrecedente()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeMiniJeuAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "Ensoleille");
        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "Orageux");

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("Orageux", etape.MonHumeur);
        Assert.Single(etape.ReponsesMeteo!);
    }

    [Fact]
    public async Task RepondreMiniJeu_SurUneEtapeNonActive_EstRefuse()
    {
        var (boardId, _, facilitateurId) = await CreerBoardAvecEtapeMiniJeuAsync(etapeMiniJeuActive: false);
        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etapeMiniJeu = state.Etapes.Single(e => e.Type == "MiniJeu");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("RepondreMiniJeu", boardId, etapeMiniJeu.Id, "Ensoleille")
        );
        Assert.Contains("active", ex.Message);
    }

    private async Task<(Guid BoardId, Guid EtapeId, Guid FacilitateurId)> CreerBoardAvecEtapeMiniJeuAsync(bool etapeMiniJeuActive = true)
    {
        var miniJeuId = await ObtenirMiniJeuIdAsync();

        IReadOnlyList<EtapeRequestDto> etapes = etapeMiniJeuActive
            ? [new EtapeRequestDto("MiniJeu", null, null, miniJeuId, null, null)]
            :
            [
                new EtapeRequestDto("ColonnesEtPostIts", null, new ThemePersonnaliseDto("Icebreaker", null, null, ["A"]), null, null, null),
                new EtapeRequestDto("MiniJeu", null, null, miniJeuId, null, null),
            ];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId, created.ParticipantId);
        var etapeId = etapeMiniJeuActive ? state.Etapes[0].Id : state.Etapes.Single(e => e.Type == "MiniJeu").Id;

        return (created.BoardId, etapeId, created.ParticipantId);
    }

    private async Task<Guid> ObtenirMiniJeuIdAsync()
    {
        var response = await _client.GetAsync("/api/mini-jeux");
        var miniJeux = await response.Content.ReadFromJsonAsync<List<MiniJeuRefDto>>();
        return miniJeux!.Single().Id;
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
