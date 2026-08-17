using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Étape de type Mini-jeu "ROTI" (specs/008-roti-mini-jeu).</summary>
public class RotiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RotiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record ReponseMiniJeuChangeeEnvelope(Guid EtapeId, Guid ParticipantId, string NomAffiche, string Reponse);

    [Fact]
    public async Task CatalogueMiniJeux_ContientRoti()
    {
        var response = await _client.GetAsync("/api/mini-jeux");
        var miniJeux = await response.Content.ReadFromJsonAsync<List<MiniJeuRefDto>>();

        Assert.Contains(miniJeux!, m => m.TypeInterne == "roti");
    }

    [Fact]
    public async Task RepondreMiniJeu_Roti_EnregistreLaReponseEtLaDiffuse()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeRotiAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<ReponseMiniJeuChangeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<ReponseMiniJeuChangeeEnvelope>("ReponseMiniJeuChangee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "Rentable");
        var reponse = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(facilitateurId, reponse.ParticipantId);
        Assert.Equal("Rentable", reponse.Reponse);

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("Rentable", etape.MonNiveauRoti);
        Assert.Single(etape.ReponsesRoti!);
    }

    [Fact]
    public async Task RepondreMiniJeu_Roti_UneSecondeFois_RemplaceLaReponsePrecedente()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeRotiAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "PerteDeTemps");
        await connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "TresRentable");

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal("TresRentable", etape.MonNiveauRoti);
        Assert.Single(etape.ReponsesRoti!);
    }

    [Fact]
    public async Task RepondreMiniJeu_Roti_AvecNiveauInconnu_EstRefuse()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapeRotiAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("RepondreMiniJeu", boardId, etapeId, "Fantastique"));
        Assert.Contains("reconnue", ex.Message);
    }

    [Fact]
    public async Task ComposerEtapeRoti_AvecPersonnalisationValide_EstRenvoyeeParGetBoard()
    {
        var rotiId = await ObtenirMiniJeuIdAsync("roti");
        IReadOnlyList<EtapeRequestDto> etapes =
        [
            new EtapeRequestDto(
                "MiniJeu",
                null,
                null,
                rotiId,
                null,
                null,
                [new NiveauVisuelDto("TresRentable", "https://example.com/tres-rentable.png")]
            ),
        ];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId, created.ParticipantId);
        var etape = state.Etapes.Single();

        var visuel = Assert.Single(etape.VisuelsRoti!);
        Assert.Equal("TresRentable", visuel.Niveau);
        Assert.Equal("https://example.com/tres-rentable.png", visuel.UrlIllustration);
    }

    [Fact]
    public async Task ComposerEtapeRoti_AvecUrlNonHttps_EstRefusee()
    {
        var rotiId = await ObtenirMiniJeuIdAsync("roti");
        IReadOnlyList<EtapeRequestDto> etapes =
        [
            new EtapeRequestDto(
                "MiniJeu",
                null,
                null,
                rotiId,
                null,
                null,
                [new NiveauVisuelDto("TresRentable", "http://example.com/insecure.png")]
            ),
        ];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ComposerEtapeMeteo_AvecPersonnalisationRoti_EstRefusee()
    {
        var meteoId = await ObtenirMiniJeuIdAsync("meteo-equipe");
        IReadOnlyList<EtapeRequestDto> etapes =
        [
            new EtapeRequestDto(
                "MiniJeu",
                null,
                null,
                meteoId,
                null,
                null,
                [new NiveauVisuelDto("TresRentable", "https://example.com/tres-rentable.png")]
            ),
        ];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(Guid BoardId, Guid EtapeId, Guid FacilitateurId)> CreerBoardAvecEtapeRotiAsync()
    {
        var rotiId = await ObtenirMiniJeuIdAsync("roti");
        IReadOnlyList<EtapeRequestDto> etapes = [new EtapeRequestDto("MiniJeu", null, null, rotiId, null, null)];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId, created.ParticipantId);
        return (created.BoardId, state.Etapes[0].Id, created.ParticipantId);
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
