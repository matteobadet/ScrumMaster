using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Étape de type Poll personnalisé (US3, specs/006-systeme-extensions-etapes).</summary>
public class PollPersonnaliseTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PollPersonnaliseTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record DecompteOptionEnvelope(Guid OptionId, int Decompte);

    private record ReponsePollPersonnaliseChangeeEnvelope(Guid EtapeId, List<DecompteOptionEnvelope> DecompteParOption);

    [Fact]
    public async Task RepondrePollPersonnalise_EnregistreLaReponseEtDecompteParOption()
    {
        var (boardId, etapeId, optionOuiId, _, facilitateurId) = await CreerBoardAvecPollAsync();
        var autreParticipantId = await JoinBoardAsync(boardId, "Sam");

        await using var connexionFacilitateur = CreateConnection();
        await connexionFacilitateur.StartAsync();
        await connexionFacilitateur.InvokeAsync("JoinBoard", boardId, facilitateurId);

        await using var connexionAutre = CreateConnection();
        await connexionAutre.StartAsync();
        await connexionAutre.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        var tcs = new TaskCompletionSource<ReponsePollPersonnaliseChangeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connexionAutre.On<ReponsePollPersonnaliseChangeeEnvelope>("ReponsePollPersonnaliseChangee", e => tcs.TrySetResult(e));

        await connexionFacilitateur.InvokeAsync("RepondrePollPersonnalise", boardId, etapeId, optionOuiId);
        var changement = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, changement.DecompteParOption.Single(d => d.OptionId == optionOuiId).Decompte);
    }

    [Fact]
    public async Task RepondrePollPersonnalise_UneSecondeFois_RemplaceLaReponsePrecedente()
    {
        var (boardId, etapeId, optionOuiId, optionNonId, facilitateurId) = await CreerBoardAvecPollAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        await connection.InvokeAsync("RepondrePollPersonnalise", boardId, etapeId, optionOuiId);

        var tcs = new TaskCompletionSource<ReponsePollPersonnaliseChangeeEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<ReponsePollPersonnaliseChangeeEnvelope>("ReponsePollPersonnaliseChangee", e => tcs.TrySetResult(e));

        await connection.InvokeAsync("RepondrePollPersonnalise", boardId, etapeId, optionNonId);
        var changement = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, changement.DecompteParOption.Single(d => d.OptionId == optionOuiId).Decompte);
        Assert.Equal(1, changement.DecompteParOption.Single(d => d.OptionId == optionNonId).Decompte);
    }

    [Fact]
    public async Task RepondrePollPersonnalise_AvecOptionInexistante_EstRefuse()
    {
        var (boardId, etapeId, _, _, facilitateurId) = await CreerBoardAvecPollAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var ex = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("RepondrePollPersonnalise", boardId, etapeId, Guid.NewGuid())
        );
        Assert.Contains("appartient pas", ex.Message);
    }

    private async Task<(Guid BoardId, Guid EtapeId, Guid OptionOuiId, Guid OptionNonId, Guid FacilitateurId)> CreerBoardAvecPollAsync()
    {
        var request = new CreateBoardRequest(
            "Krypton",
            "Sprint-1",
            null,
            null,
            null,
            "Alex",
            [new EtapeRequestDto("PollPersonnalise", null, null, null, "On garde la mêlée ?", ["Oui", "Non"])]
        );
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var stateResponse = await _client.GetAsync($"/api/boards/{created!.BoardId}");
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();
        var etape = state!.Etapes[0];

        return (created.BoardId, etape.Id, etape.Options![0].Id, etape.Options![1].Id, created.ParticipantId);
    }

    private async Task<Guid> JoinBoardAsync(Guid boardId, string nomAffiche)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/participants", new JoinBoardRequest(nomAffiche));
        var joined = await response.Content.ReadFromJsonAsync<JoinBoardResponse>();
        return joined!.ParticipantId;
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
