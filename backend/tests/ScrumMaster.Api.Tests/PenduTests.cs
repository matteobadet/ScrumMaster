using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Étape de type Mini-jeu "Pendu" (specs/011-pendu-lien-externe).</summary>
public class PenduTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PenduTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record LettrePenduProposeeEnvelope(
        Guid EtapeId,
        string Lettre,
        bool Correcte,
        List<string?> MotMasquePendu,
        int EssaisRestantsPendu,
        int MaxEssaisPendu,
        string EtatPendu,
        string? MotCompletPendu
    );

    [Fact]
    public async Task CatalogueMiniJeux_ContientPenduEtLienExterne()
    {
        var response = await _client.GetAsync("/api/mini-jeux");
        var miniJeux = await response.Content.ReadFromJsonAsync<List<MiniJeuRefDto>>();

        Assert.Contains(miniJeux!, m => m.TypeInterne == "pendu");
        Assert.Contains(miniJeux!, m => m.TypeInterne == "lien-externe");
    }

    [Fact]
    public async Task ComposerEtapePendu_SansMot_EstRefusee()
    {
        var penduId = await ObtenirMiniJeuIdAsync("pendu");
        IReadOnlyList<EtapeRequestDto> etapes = [new EtapeRequestDto("MiniJeu", null, null, penduId, null, null)];

        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex", etapes);
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ComposerEtapePendu_MotMasqueInitial_NeRevelePasLeMot()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("CHAT");

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);

        Assert.Equal(4, etape.MotMasquePendu!.Count);
        Assert.All(etape.MotMasquePendu!, c => Assert.Null(c));
        Assert.Equal(6, etape.EssaisRestantsPendu);
        Assert.Equal(6, etape.MaxEssaisPendu);
        Assert.Equal("EnCours", etape.EtatPendu);
        Assert.Null(etape.MotCompletPendu);
    }

    [Fact]
    public async Task ProposerLettrePendu_LettreCorrecte_RevleToutesSesOccurrences()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("BALLE");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<LettrePenduProposeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<LettrePenduProposeeEnvelope>("LettrePenduProposee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "L");
        var evt = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(evt.Correcte);
        Assert.Equal([null, null, "L", "L", null], evt.MotMasquePendu);
        Assert.Equal(6, evt.EssaisRestantsPendu);
        Assert.Equal("EnCours", evt.EtatPendu);
    }

    [Fact]
    public async Task ProposerLettrePendu_LettreIncorrecte_DecrementeLesEssais()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("CHAT");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var tcs = new TaskCompletionSource<LettrePenduProposeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<LettrePenduProposeeEnvelope>("LettrePenduProposee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "Z");
        var evt = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(evt.Correcte);
        Assert.Equal(5, evt.EssaisRestantsPendu);
    }

    [Fact]
    public async Task ProposerLettrePendu_LettreDejaProposee_EstIgnoreeSansConsequence()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("CHAT");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "Z");

        var recus = new List<LettrePenduProposeeEnvelope>();
        using var sub = connection.On<LettrePenduProposeeEnvelope>("LettrePenduProposee", recus.Add);

        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "Z");
        await Task.Delay(200);

        Assert.Empty(recus);

        var state = await GetBoardStateAsync(boardId, facilitateurId);
        var etape = state.Etapes.Single(e => e.Id == etapeId);
        Assert.Equal(5, etape.EssaisRestantsPendu);
    }

    [Fact]
    public async Task ProposerLettrePendu_MotComplet_DeclareLaVictoire()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("CHAT");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        foreach (var lettre in new[] { "C", "H", "A" })
        {
            await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, lettre);
        }

        var tcs = new TaskCompletionSource<LettrePenduProposeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<LettrePenduProposeeEnvelope>("LettrePenduProposee", e => tcs.TrySetResult(e));
        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "T");
        var evt = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Victoire", evt.EtatPendu);
        Assert.Equal("CHAT", evt.MotCompletPendu);
    }

    [Fact]
    public async Task ProposerLettrePendu_EssaisEpuises_DeclareLaDefaiteEtRevelateLeMot()
    {
        var (boardId, etapeId, facilitateurId) = await CreerBoardAvecEtapePenduAsync("CHAT");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        foreach (var lettre in new[] { "Z", "X", "Q", "W", "K" })
        {
            await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, lettre);
        }

        var tcs = new TaskCompletionSource<LettrePenduProposeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<LettrePenduProposeeEnvelope>("LettrePenduProposee", e => tcs.TrySetResult(e));
        await connection.InvokeAsync("ProposerLettrePendu", boardId, etapeId, "J");
        var evt = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Defaite", evt.EtatPendu);
        Assert.Equal(0, evt.EssaisRestantsPendu);
        Assert.Equal("CHAT", evt.MotCompletPendu);
    }

    private async Task<(Guid BoardId, Guid EtapeId, Guid FacilitateurId)> CreerBoardAvecEtapePenduAsync(string mot)
    {
        var penduId = await ObtenirMiniJeuIdAsync("pendu");
        IReadOnlyList<EtapeRequestDto> etapes = [new EtapeRequestDto("MiniJeu", null, null, penduId, null, null, null, mot)];

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
