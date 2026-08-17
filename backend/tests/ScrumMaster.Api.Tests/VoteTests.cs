using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class VoteTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VoteTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record VoteChangedEnvelope(Guid PostItId, int NombreVotes);

    [Fact]
    public async Task Vote_PuisRemoveVote_MetAJourLeDecompteEtLeQuota()
    {
        var (boardId, colonneId, participantId) = await CreateBoardAsync(maxVotes: 2);
        var postItId = await AddPostItAsync(boardId, colonneId, participantId);

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        var voteChanged = new TaskCompletionSource<VoteChangedEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<VoteChangedEnvelope>("VoteChanged", e => voteChanged.TrySetResult(e));

        await connection.InvokeAsync("Vote", boardId, postItId);
        var afterVote = await voteChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, afterVote.NombreVotes);

        var stateAfterVote = await GetBoardStateAsync(boardId, participantId);
        Assert.True(stateAfterVote.Etapes[0].PostIts!.Single(p => p.Id == postItId).VoteDuParticipant);
        Assert.Equal(1, stateAfterVote.Etapes[0].MesVotesRestants);

        voteChanged = new TaskCompletionSource<VoteChangedEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        await connection.InvokeAsync("RemoveVote", boardId, postItId);
        var afterRemove = await voteChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, afterRemove.NombreVotes);

        var stateAfterRemove = await GetBoardStateAsync(boardId, participantId);
        Assert.False(stateAfterRemove.Etapes[0].PostIts!.Single(p => p.Id == postItId).VoteDuParticipant);
        Assert.Equal(2, stateAfterRemove.Etapes[0].MesVotesRestants);
    }

    [Fact]
    public async Task Vote_AuDelaDuQuota_EstRefuse()
    {
        var (boardId, colonneId, participantId) = await CreateBoardAsync(maxVotes: 1);
        var postIt1 = await AddPostItAsync(boardId, colonneId, participantId);
        var postIt2 = await AddPostItAsync(boardId, colonneId, participantId);

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        await connection.InvokeAsync("Vote", boardId, postIt1);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Vote", boardId, postIt2));
        Assert.Contains("limite", ex.Message);
    }

    [Fact]
    public async Task Vote_DeuxFoisPourLeMemePostIt_EstRefuse()
    {
        var (boardId, colonneId, participantId) = await CreateBoardAsync(maxVotes: 3);
        var postItId = await AddPostItAsync(boardId, colonneId, participantId);

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        await connection.InvokeAsync("Vote", boardId, postItId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Vote", boardId, postItId));
        Assert.Contains("déjà voté", ex.Message);
    }

    private async Task<BoardStateDto> GetBoardStateAsync(Guid boardId, Guid participantId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}?asParticipantId={participantId}");
        return (await response.Content.ReadFromJsonAsync<BoardStateDto>())!;
    }

    private async Task<Guid> AddPostItAsync(Guid boardId, Guid colonneId, Guid participantId)
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        var tcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<PostItAddedEnvelope>("PostItAdded", e => tcs.TrySetResult(e.PostIt.Id));

        await connection.InvokeAsync("AddPostIt", boardId, colonneId, "Post-it pour le vote");

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private record PostItAddedEnvelope(PostItPayload PostIt);

    private record PostItPayload(Guid Id);

    private async Task<(Guid BoardId, Guid ColonneId, Guid ParticipantId)> CreateBoardAsync(int maxVotes)
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, maxVotes, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var state = await GetBoardStateAsync(created!.BoardId, created.ParticipantId);

        return (created.BoardId, state.Etapes[0].Colonnes![0].Id, created.ParticipantId);
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
