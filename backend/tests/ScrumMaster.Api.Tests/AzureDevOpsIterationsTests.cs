using System.Net;
using System.Net.Http.Json;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class AzureDevOpsIterationsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AzureDevOpsIterationsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListerEquipesConfigurees_NeListeQueLesEquipesAvecUneConfiguration()
    {
        await CreerEquipeAsync("Krypton");
        await CreerEquipeAsync("SansConfig");
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);
        await _client.PutAsJsonAsync(
            "/api/equipes/Krypton/azure-devops-config",
            new AzureDevOpsConfigRequest("org", "Projet", "pat")
        );

        var response = await _client.GetAsync("/api/equipes/avec-azure-devops");
        var equipes = await response.Content.ReadFromJsonAsync<List<EquipeAzureDevOpsDto>>();

        Assert.Contains(equipes!, e => e.AreaPath == "Krypton");
        Assert.DoesNotContain(equipes!, e => e.AreaPath == "SansConfig");
    }

    [Fact]
    public async Task ObtenirIterations_IndiqueLIterationEnCoursCalculeeDepuisLesDates()
    {
        await CreerEquipeAsync("Krypton");
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);
        await _client.PutAsJsonAsync(
            "/api/equipes/Krypton/azure-devops-config",
            new AzureDevOpsConfigRequest("org", "Projet", "pat")
        );

        var aujourdhui = DateTimeOffset.UtcNow;
        var arbre = new
        {
            name = "Projet",
            children = new object[]
            {
                new
                {
                    name = "Sprint 137",
                    attributes = new { startDate = aujourdhui.AddDays(-20), finishDate = aujourdhui.AddDays(-7) },
                },
                new
                {
                    name = "Sprint 138",
                    attributes = new { startDate = aujourdhui.AddDays(-3), finishDate = aujourdhui.AddDays(11) },
                },
            },
        };
        _factory.AzureDevOpsHandler.Repondre = _ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(arbre);
            return response;
        };

        var response = await _client.GetAsync("/api/equipes/Krypton/azure-devops/iterations");
        var iterations = await response.Content.ReadFromJsonAsync<List<IterationAzureDevOpsDto>>();

        Assert.Equal(2, iterations!.Count);
        Assert.Single(iterations, i => i.EnCours);
        Assert.Equal("Projet\\Sprint 138", iterations.Single(i => i.EnCours).CheminIteration);
    }

    [Fact]
    public async Task ObtenirIterations_SansConfiguration_Retourne404()
    {
        await CreerEquipeAsync("Krypton");

        var response = await _client.GetAsync("/api/equipes/Krypton/azure-devops/iterations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ObtenirIterations_QuandAzureDevOpsEstInjoignable_Retourne502()
    {
        await CreerEquipeAsync("Krypton");
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);
        await _client.PutAsJsonAsync(
            "/api/equipes/Krypton/azure-devops-config",
            new AzureDevOpsConfigRequest("org", "Projet", "pat")
        );
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var response = await _client.GetAsync("/api/equipes/Krypton/azure-devops/iterations");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private async Task CreerEquipeAsync(string areaPath)
    {
        var request = new CreateBoardRequest(areaPath, "Sprint-1", null, null, null, "Alex");
        var response = await _client.PostAsJsonAsync("/api/boards", request);
        response.EnsureSuccessStatusCode();
    }
}
