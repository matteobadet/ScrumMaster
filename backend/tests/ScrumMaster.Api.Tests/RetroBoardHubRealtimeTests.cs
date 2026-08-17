using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class RetroBoardHubRealtimeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RetroBoardHubRealtimeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record PostItAddedEnvelope(PostItPayload PostIt);

    private record PostItPayload(Guid Id, Guid ColonneId, string Texte, string Auteur, int NombreVotes);

    private record PostItMovedEnvelope(Guid PostItId, Guid ColonneId);

    [Fact]
    public async Task AddPostIt_ParUnParticipant_EstDiffuseAuSecondParticipantConnecte()
    {
        var (boardId, colonnes, facilitateurId) = await CreateBoardAsync();
        var autreParticipantId = await JoinBoardAsync(boardId, "Sam");

        await using var facilitateurConn = CreateConnection();
        await using var observateurConn = CreateConnection();

        var receivedTcs = new TaskCompletionSource<PostItPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        observateurConn.On<PostItAddedEnvelope>("PostItAdded", envelope => receivedTcs.TrySetResult(envelope.PostIt));

        await facilitateurConn.StartAsync();
        await facilitateurConn.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await observateurConn.StartAsync();
        await observateurConn.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        await facilitateurConn.InvokeAsync("AddPostIt", boardId, colonnes[0], "Vu par tout le monde");

        var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Vu par tout le monde", received.Texte);
        Assert.Equal("Alex", received.Auteur);
    }

    [Fact]
    public async Task MovePostIt_EstDiffuseATousLesParticipantsDuBoard()
    {
        var (boardId, colonnes, facilitateurId) = await CreateBoardAsync();
        var autreParticipantId = await JoinBoardAsync(boardId, "Sam");

        await using var facilitateurConn = CreateConnection();
        await facilitateurConn.StartAsync();
        await facilitateurConn.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var addedTcs = new TaskCompletionSource<PostItPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var addedSub = facilitateurConn.On<PostItAddedEnvelope>("PostItAdded", e => addedTcs.TrySetResult(e.PostIt));
        await facilitateurConn.InvokeAsync("AddPostIt", boardId, colonnes[0], "À déplacer");
        var postIt = await addedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await using var observateurConn = CreateConnection();
        var movedTcs = new TaskCompletionSource<PostItMovedEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        observateurConn.On<PostItMovedEnvelope>("PostItMoved", e => movedTcs.TrySetResult(e));
        await observateurConn.StartAsync();
        await observateurConn.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        // Un participant qui n'est pas l'auteur peut déplacer un post-it (FR-006 ne restreint pas au seul auteur).
        await observateurConn.InvokeAsync("MovePostIt", boardId, postIt.Id, colonnes[1]);

        var moved = await movedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(postIt.Id, moved.PostItId);
        Assert.Equal(colonnes[1], moved.ColonneId);
    }

    private async Task<Guid> JoinBoardAsync(Guid boardId, string nomAffiche)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/participants", new JoinBoardRequest(nomAffiche));
        var joined = await response.Content.ReadFromJsonAsync<JoinBoardResponse>();
        return joined!.ParticipantId;
    }

    private async Task<(Guid BoardId, List<Guid> Colonnes, Guid FacilitateurId)> CreateBoardAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var stateResponse = await _client.GetAsync($"/api/boards/{created!.BoardId}");
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        return (created.BoardId, state!.Etapes[0].Colonnes!.Select(c => c.Id).ToList(), created.ParticipantId);
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
