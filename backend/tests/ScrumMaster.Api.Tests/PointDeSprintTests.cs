using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

/// <summary>Panneau "Point de sprint" — statistiques Azure DevOps (specs/009-sprint-review-stats).</summary>
public class PointDeSprintTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public PointDeSprintTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private static readonly object[] EtatsAgile =
    [
        new { name = "New", category = "Proposed" },
        new { name = "Active", category = "InProgress" },
        new { name = "Resolved", category = "Resolved" },
        new { name = "Closed", category = "Completed" },
        new { name = "Removed", category = "Removed" },
    ];

    [Fact]
    public async Task PointDeSprint_RepartitionParEtat_CorrespondAuxDonneesReelles()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStub(
            [
                new { id = 1, type = "Task", etat = "New" },
                new { id = 2, type = "Task", etat = "Active" },
                new { id = 3, type = "Task", etat = "Closed" },
            ]
        );

        var response = await _client.GetAsync($"/api/boards/{boardId}/point-de-sprint?asParticipantId={facilitateurId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<PointDeSprintDto>();

        var task = Assert.Single(stats!.RepartitionParType);
        Assert.Equal("Task", task.Type);
        Assert.Equal(1, task.AFaire);
        Assert.Equal(1, task.EnCours);
        Assert.Equal(1, task.Termine);
    }

    [Fact]
    public async Task PointDeSprint_IterationVide_RenvoieUnEtatVide()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStub([]);

        var response = await _client.GetAsync($"/api/boards/{boardId}/point-de-sprint?asParticipantId={facilitateurId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<PointDeSprintDto>();

        Assert.Empty(stats!.RepartitionParType);
        Assert.Equal(0, stats.TotalPlanifie);
        Assert.Equal(0, stats.TotalTermine);
    }

    [Fact]
    public async Task PointDeSprint_EquipeNonConfiguree_EstRefuse()
    {
        var request = new CreateBoardRequest("Krypton-NonConfiguree", "Sprint-1", null, null, null, "Alex");
        var createResponse = await _client.PostAsJsonAsync("/api/boards", request);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        var response = await _client.GetAsync($"/api/boards/{created!.BoardId}/point-de-sprint?asParticipantId={created.ParticipantId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PointDeSprint_DistingueTaskEtUserStory_SansSectionVidePourLeTypeAbsent()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStub(
            [
                new { id = 1, type = "Task", etat = "New" },
                new { id = 2, type = "User Story", etat = "Closed" },
            ]
        );

        var response = await _client.GetAsync($"/api/boards/{boardId}/point-de-sprint?asParticipantId={facilitateurId}");
        var stats = await response.Content.ReadFromJsonAsync<PointDeSprintDto>();

        Assert.Equal(2, stats!.RepartitionParType.Count);
        Assert.Contains(stats.RepartitionParType, r => r.Type == "Task" && r.AFaire == 1);
        Assert.Contains(stats.RepartitionParType, r => r.Type == "UserStory" && r.Termine == 1);
    }

    [Fact]
    public async Task PointDeSprint_TotalPlanifieEtTermine_ExcluentLesWorkItemsRemoved()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStub(
            [
                new { id = 1, type = "Task", etat = "New" },
                new { id = 2, type = "Task", etat = "Closed" },
                new { id = 3, type = "Task", etat = "Removed" },
            ]
        );

        var response = await _client.GetAsync($"/api/boards/{boardId}/point-de-sprint?asParticipantId={facilitateurId}");
        var stats = await response.Content.ReadFromJsonAsync<PointDeSprintDto>();

        Assert.Equal(2, stats!.TotalPlanifie);
        Assert.Equal(1, stats.TotalTermine);
    }

    [Fact]
    public async Task PointDeSprint_RestNAccessibleApresLaClotureDuBoard()
    {
        var (boardId, facilitateurId) = await CreerBoardConfigureAsync();
        ConfigurerStub([new { id = 1, type = "Task", etat = "New" }]);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/retro-board",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                }
            )
            .Build();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinBoard", boardId, facilitateurId);
        await connection.InvokeAsync("AvancerEtape", boardId);

        var response = await _client.GetAsync($"/api/boards/{boardId}/point-de-sprint?asParticipantId={facilitateurId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private void ConfigurerStub(object[] workItems)
    {
        _factory.AzureDevOpsHandler.Repondre = request =>
        {
            var chemin = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);

            if (chemin.Contains("_apis/wit/wiql"))
            {
                var wiql = new { workItems = workItems.Select(w => new { id = (int)w.GetType().GetProperty("id")!.GetValue(w)! }).ToArray() };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(wiql) };
            }

            if (chemin.Contains("_apis/wit/workitems") && request.Method == HttpMethod.Get)
            {
                var lot = new
                {
                    value = workItems
                        .Select(w => new
                        {
                            id = (int)w.GetType().GetProperty("id")!.GetValue(w)!,
                            fields = new Dictionary<string, object>
                            {
                                ["System.Title"] = "Titre",
                                ["System.WorkItemType"] = (string)w.GetType().GetProperty("type")!.GetValue(w)!,
                                ["System.State"] = (string)w.GetType().GetProperty("etat")!.GetValue(w)!,
                            },
                        })
                        .ToArray(),
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(lot) };
            }

            if (chemin.Contains("/states"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { value = EtatsAgile }) };
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
}
