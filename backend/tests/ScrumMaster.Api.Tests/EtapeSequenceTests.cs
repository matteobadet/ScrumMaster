using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Composition explicite d'une séquence de plusieurs étapes (US1, specs/006-systeme-extensions-etapes).</summary>
public class EtapeSequenceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EtapeSequenceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record EtapeChangeeEnvelope(Guid NouvelleEtapeId);

    [Fact]
    public async Task CreateBoard_AvecSequenceExplicite_SeuleLaPremiereEtapeEstActive()
    {
        var request = new CreateBoardRequest(
            AreaPath: "Krypton",
            Iteration: "Sprint-1",
            ThemeId: null,
            ThemePersonnalise: null,
            MaxVotesParParticipant: null,
            NomAffiche: "Alex",
            Etapes:
            [
                new EtapeRequestDto("ColonnesEtPostIts", null, new ThemePersonnaliseDto("Icebreaker", null, null, ["Météo"]), null, null, null),
                new EtapeRequestDto("ColonnesEtPostIts", null, new ThemePersonnaliseDto("Retro", null, null, ["Start", "Stop"]), null, null, null),
            ]
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId);

        Assert.Equal(2, state.Etapes.Count);
        Assert.Equal("Active", state.Etapes[0].Statut);
        Assert.Equal("AVenir", state.Etapes[1].Statut);
        Assert.Equal("Icebreaker", state.Etapes[0].Theme!.Nom);
        Assert.Equal("Retro", state.Etapes[1].Theme!.Nom);
    }

    [Fact]
    public async Task AvancerEtape_ActiveLaSuivanteEtDiffuseEtapeChangee_EtapePrecedenteResteConsultable()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-1",
            null,
            null,
            null,
            "Alex",
            Etapes:
            [
                new EtapeRequestDto("ColonnesEtPostIts", null, new ThemePersonnaliseDto("Icebreaker", null, null, ["Météo"]), null, null, null),
                new EtapeRequestDto("ColonnesEtPostIts", null, new ThemePersonnaliseDto("Retro", null, null, ["Start", "Stop"]), null, null, null),
            ]
        );

        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var etatInitial = await GetBoardStateAsync(created!.BoardId);
        var premiereEtapeId = etatInitial.Etapes[0].Id;
        var deuxiemeEtapeId = etatInitial.Etapes[1].Id;

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", created.BoardId, created.ParticipantId);

        var tcs = new TaskCompletionSource<EtapeChangeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<EtapeChangeeEnvelope>("EtapeChangee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("AvancerEtape", created.BoardId);
        var changee = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(deuxiemeEtapeId, changee.NouvelleEtapeId);

        var etatApres = await GetBoardStateAsync(created.BoardId);
        Assert.Equal("Terminee", etatApres.Etapes.Single(e => e.Id == premiereEtapeId).Statut);
        Assert.Equal("Active", etatApres.Etapes.Single(e => e.Id == deuxiemeEtapeId).Statut);
        // FR-007 : l'étape terminée reste consultable (son thème/colonnes sont toujours renvoyés).
        Assert.Equal("Icebreaker", etatApres.Etapes.Single(e => e.Id == premiereEtapeId).Theme!.Nom);
        Assert.Equal("Actif", etatApres.Statut);
    }

    [Fact]
    public async Task CreateBoard_AvecSequenceVide_EstRefuse()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", Etapes: []);

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<BoardStateDto> GetBoardStateAsync(Guid boardId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}");
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
