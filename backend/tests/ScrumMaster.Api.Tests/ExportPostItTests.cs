using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class ExportPostItTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ExportPostItTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private record PostItAddedEnvelope(PostItPayload PostIt);

    private record PostItPayload(Guid Id);

    private record PostItExportedEnvelope(Guid PostItId, int WorkItemId);

    [Fact]
    public async Task ExportPostIt_CreeUnWorkItemEtMarqueLePostItExporte()
    {
        var (boardId, facilitateurId, colonneId) = await CreerBoardConfigureAsync();

        await using var connection = CreerConnexion();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var postItId = await AjouterPostItAsync(connection, boardId, colonneId);

        _factory.AzureDevOpsHandler.Repondre = request =>
            request.RequestUri!.AbsolutePath.Contains("_apis/wit/workitems/$Task")
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 555 }) }
                : new HttpResponseMessage(HttpStatusCode.OK);

        var exportTcs = new TaskCompletionSource<PostItExportedEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<PostItExportedEnvelope>("PostItExported", e => exportTcs.TrySetResult(e));

        await connection.InvokeAsync("ExportPostIt", boardId, postItId);
        var exporte = await exportTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(postItId, exporte.PostItId);
        Assert.Equal(555, exporte.WorkItemId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScrumMasterDbContext>();
        var postIt = await db.PostIts.SingleAsync(p => p.Id == postItId);
        Assert.Equal(555, postIt.WorkItemExporteId);
    }

    [Fact]
    public async Task ExportPostIt_DejaExporte_EstRefuseSansCreerUnSecondWorkItem()
    {
        var (boardId, facilitateurId, colonneId) = await CreerBoardConfigureAsync();

        await using var connection = CreerConnexion();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var postItId = await AjouterPostItAsync(connection, boardId, colonneId);

        var appelsCreation = 0;
        _factory.AzureDevOpsHandler.Repondre = request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("_apis/wit/workitems/$Task"))
            {
                Interlocked.Increment(ref appelsCreation);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 555 }) };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        await connection.InvokeAsync("ExportPostIt", boardId, postItId);

        var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("ExportPostIt", boardId, postItId));

        Assert.Contains("déjà été exporté", ex.Message);
        Assert.Equal(1, appelsCreation);
    }

    private async Task<Guid> AjouterPostItAsync(HubConnection connection, Guid boardId, Guid colonneId)
    {
        var addedTcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<PostItAddedEnvelope>("PostItAdded", e => addedTcs.TrySetResult(e.PostIt.Id));
        await connection.InvokeAsync("AddPostIt", boardId, colonneId, "Action à mener");
        return await addedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task<(Guid BoardId, Guid FacilitateurId, Guid ColonneId)> CreerBoardConfigureAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);
        await _client.PutAsJsonAsync("/api/equipes/Krypton/azure-devops-config", new AzureDevOpsConfigRequest("org", "Projet", "pat"));

        var stateResponse = await _client.GetAsync($"/api/boards/{created!.BoardId}");
        var state = await stateResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        return (created.BoardId, created.ParticipantId, state!.Etapes[0].Colonnes![0].Id);
    }

    private HubConnection CreerConnexion() =>
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
