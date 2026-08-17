using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class BoardClosureTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BoardClosureTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record PostItAddedEnvelope(PostItPayload PostIt);

    private record PostItPayload(Guid Id);

    [Fact]
    public async Task CloseBoard_ParUnNonFacilitateur_EstRefuse()
    {
        var (boardId, _, _) = await CreateBoardWithPostItAsync();
        var autreParticipantId = await JoinBoardAsync(boardId, "Sam");

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("AvancerEtape", boardId));

        Assert.Contains("facilitateur", ex.Message);
    }

    [Fact]
    public async Task CloseBoard_ParLeFacilitateur_PasseLeBoardEnLectureSeule()
    {
        var (boardId, _, facilitateurId) = await CreateBoardWithPostItAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var closedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<object>("BoardClosed", _ => closedTcs.TrySetResult(true));

        await connection.InvokeAsync("AvancerEtape", boardId);
        await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var response = await _client.GetAsync($"/api/boards/{boardId}");
        var state = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        Assert.Equal("Cloture", state!.Statut);
    }

    [Fact]
    public async Task CloseBoard_Deja_EstRefuse()
    {
        var (boardId, _, facilitateurId) = await CreateBoardWithPostItAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("AvancerEtape", boardId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("AvancerEtape", boardId));
        Assert.Contains("clôturé", ex.Message);
    }

    [Theory]
    [InlineData("AddPostIt")]
    [InlineData("EditPostIt")]
    [InlineData("MovePostIt")]
    [InlineData("DeletePostIt")]
    [InlineData("Vote")]
    [InlineData("RemoveVote")]
    [InlineData("ChangeTheme")]
    public async Task ToutesLesMutations_SurUnBoardCloture_SontRefusees(string methode)
    {
        var (boardId, colonnes, facilitateurId) = await CreateBoardWithPostItAsync();
        var postItId = await GetFirstPostItIdAsync(boardId);

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("AvancerEtape", boardId);

        var invocation = methode switch
        {
            "AddPostIt" => connection.InvokeAsync(methode, boardId, colonnes[0], "Trop tard"),
            "EditPostIt" => connection.InvokeAsync(methode, boardId, postItId, "Trop tard"),
            "MovePostIt" => connection.InvokeAsync(methode, boardId, postItId, colonnes[1]),
            "DeletePostIt" => connection.InvokeAsync(methode, boardId, postItId),
            "Vote" => connection.InvokeAsync(methode, boardId, postItId),
            "RemoveVote" => connection.InvokeAsync(methode, boardId, postItId),
            "ChangeTheme" => connection.InvokeAsync(
                methode,
                boardId,
                null,
                new ThemePersonnaliseDto("X", null, null, [new ColonneSummaireDto("A", null, null)])
            ),
            _ => throw new InvalidOperationException(),
        };

        var ex = await Assert.ThrowsAsync<HubException>(() => invocation);

        // AddPostIt/ChangeTheme résolvent l'étape via le board et rejettent au niveau board
        // ("clôturé") ; les autres mutations résolvent directement le post-it/vote et rejettent
        // au niveau de l'étape (devenue Terminee en même temps que le board se clôture, puisque
        // ce board n'a qu'une seule étape — specs/006-systeme-extensions-etapes).
        var messageAttendu = methode is "AddPostIt" or "ChangeTheme" ? "clôturé" : "plus active";
        Assert.Contains(messageAttendu, ex.Message);
    }

    private async Task<Guid> GetFirstPostItIdAsync(Guid boardId)
    {
        var response = await _client.GetAsync($"/api/boards/{boardId}");
        var state = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        return state!.Etapes[0].PostIts![0].Id;
    }

    private async Task<Guid> JoinBoardAsync(Guid boardId, string nomAffiche)
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/participants", new JoinBoardRequest(nomAffiche));
        var joined = await response.Content.ReadFromJsonAsync<JoinBoardResponse>();
        return joined!.ParticipantId;
    }

    private async Task<(Guid BoardId, List<Guid> Colonnes, Guid FacilitateurId)> CreateBoardWithPostItAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var stateResponse = await _client.GetAsync($"/api/boards/{created!.BoardId}");
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();
        var colonnes = state!.Etapes[0].Colonnes!.Select(c => c.Id).ToList();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", created.BoardId, created.ParticipantId);

        var addedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var sub = connection.On<PostItAddedEnvelope>("PostItAdded", _ => addedTcs.TrySetResult(true)))
        {
            await connection.InvokeAsync("AddPostIt", created.BoardId, colonnes[0], "Un post-it");
            await addedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        return (created.BoardId, colonnes, created.ParticipantId);
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
