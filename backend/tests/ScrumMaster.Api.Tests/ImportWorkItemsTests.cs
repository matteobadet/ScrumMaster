using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class ImportWorkItemsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ImportWorkItemsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private record PostItPayload(Guid Id, Guid ColonneId, string Texte, string Auteur, Guid AuteurParticipantId);

    private record PostItAddedEnvelope(PostItPayload PostIt);

    [Fact]
    public async Task ImportWorkItems_CreeUnPostItParWorkItemNonDejaImporte()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStubImport();

        await using var connection = CreerConnexion();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var recus = new List<PostItAddedEnvelope>();
        var deuxRecus = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = connection.On<PostItAddedEnvelope>(
            "PostItAdded",
            e =>
            {
                recus.Add(e);
                if (recus.Count >= 2)
                {
                    deuxRecus.TrySetResult();
                }
            }
        );

        await connection.InvokeAsync("ImportWorkItems", boardId);
        await deuxRecus.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, recus.Count);
        Assert.Contains(recus, r => r.PostIt.Texte == "Corriger le bug X");
        Assert.Contains(recus, r => r.PostIt.Texte == "Ajouter la fonctionnalité Y");
    }

    [Fact]
    public async Task ImportWorkItems_ReimporteSansDoublon()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStubImport();

        await using var connection = CreerConnexion();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var recus = new List<PostItAddedEnvelope>();
        using var sub = connection.On<PostItAddedEnvelope>("PostItAdded", recus.Add);

        await connection.InvokeAsync("ImportWorkItems", boardId);
        await Task.Delay(200);
        await connection.InvokeAsync("ImportWorkItems", boardId);
        await Task.Delay(200);

        Assert.Equal(2, recus.Count);
    }

    [Fact]
    public async Task ImportWorkItems_SansWorkItemTrouve_NeCreeAucunPostIt()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        _factory.AzureDevOpsHandler.Repondre = request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("_apis/wit/wiql"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { workItems = Array.Empty<object>() }) };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        await using var connection = CreerConnexion();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);

        var recus = new List<PostItAddedEnvelope>();
        using var sub = connection.On<PostItAddedEnvelope>("PostItAdded", recus.Add);

        await connection.InvokeAsync("ImportWorkItems", boardId);
        await Task.Delay(200);

        Assert.Empty(recus);
    }

    private void ConfigurerStubImport()
    {
        _factory.AzureDevOpsHandler.Repondre = request =>
        {
            var chemin = request.RequestUri!.AbsolutePath;
            if (chemin.Contains("_apis/wit/wiql"))
            {
                var wiql = new { workItems = new[] { new { id = 101 }, new { id = 102 } } };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(wiql) };
            }

            if (chemin.Contains("_apis/wit/workitems") && request.Method == HttpMethod.Get)
            {
                var lot = new
                {
                    value = new object[]
                    {
                        new { id = 101, fields = new Dictionary<string, object> { ["System.Title"] = "Corriger le bug X" } },
                        new { id = 102, fields = new Dictionary<string, object> { ["System.Title"] = "Ajouter la fonctionnalité Y" } },
                    },
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(lot) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };
    }

    private async Task<(Guid BoardId, Guid FacilitateurId)> CreerBoardConfigureAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);
        await _client.PutAsJsonAsync("/api/equipes/Krypton/azure-devops-config", new AzureDevOpsConfigRequest("org", "Projet", "pat"));

        return (created!.BoardId, created.ParticipantId);
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
