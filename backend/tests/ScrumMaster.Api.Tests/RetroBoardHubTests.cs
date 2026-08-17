using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Models;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class RetroBoardHubTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RetroBoardHubTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private record PostItPayload(Guid Id, Guid ColonneId, string Texte, string Auteur, int NombreVotes);

    private record PostItAddedEnvelope(PostItPayload PostIt);

    [Fact]
    public async Task AddPostIt_AvecTexteVide_LeveUneErreur()
    {
        var (boardId, colonneId, participantId) = await CreateBoardAsync();
        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, participantId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("AddPostIt", boardId, colonneId, ""));

        Assert.Contains("vide", ex.Message);
    }

    [Fact]
    public async Task EditPostIt_ParUnNonAuteur_EstRefuse()
    {
        var (boardId, colonneId, facilitateurId) = await CreateBoardAsync();
        var autreParticipantId = await AddSecondParticipantAsync(boardId);

        await using var facilitateurConn = CreateConnection();
        await facilitateurConn.StartAsync();
        await facilitateurConn.InvokeAsync("JoinBoard", boardId, facilitateurId);
        var postItId = await AddPostItAndCaptureId(facilitateurConn, boardId, colonneId, "Retro item");

        await using var autreConn = CreateConnection();
        await autreConn.StartAsync();
        await autreConn.InvokeAsync("JoinBoard", boardId, autreParticipantId);

        var ex = await Assert.ThrowsAsync<HubException>(
            () => autreConn.InvokeAsync("EditPostIt", boardId, postItId, "Modifié par un tiers")
        );

        Assert.Contains("auteur", ex.Message);
    }

    [Fact]
    public async Task AddPostIt_PuisEditPostIt_ParSonAuteur_EstAccepte()
    {
        var (boardId, colonneId, facilitateurId) = await CreateBoardAsync();

        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        var postItId = await AddPostItAndCaptureId(connection, boardId, colonneId, "Premier jet");

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = connection.On<PostItUpdatedEnvelope>(
            "PostItUpdated",
            envelope => tcs.TrySetResult(envelope.Texte)
        );
        await connection.InvokeAsync("EditPostIt", boardId, postItId, "Version corrigée");

        var texte = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Version corrigée", texte);
    }

    private record PostItUpdatedEnvelope(Guid PostItId, string Texte);

    private async Task<Guid> AddPostItAndCaptureId(HubConnection connection, Guid boardId, Guid colonneId, string texte)
    {
        var tcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = connection.On<PostItAddedEnvelope>(
            "PostItAdded",
            envelope => tcs.TrySetResult(envelope.PostIt.Id)
        );

        await connection.InvokeAsync("AddPostIt", boardId, colonneId, texte);

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task<Guid> AddSecondParticipantAsync(Guid boardId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScrumMasterDbContext>();
        var participant = new Participant
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            NomAffiche = "Sam",
            Role = ParticipantRole.Participant,
        };
        db.Participants.Add(participant);
        await db.SaveChangesAsync();
        return participant.Id;
    }

    private async Task<(Guid BoardId, Guid ColonneId, Guid FacilitateurId)> CreateBoardAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var stateResponse = await _client.GetAsync($"/api/boards/{created!.BoardId}");
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        return (created.BoardId, state!.Etapes[0].Colonnes![0].Id, created.ParticipantId);
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
