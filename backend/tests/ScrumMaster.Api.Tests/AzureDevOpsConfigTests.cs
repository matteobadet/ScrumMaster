using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class AzureDevOpsConfigTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AzureDevOpsConfigTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Configurer_AvecUnPatValide_EnregistreLaConfigurationSansJamaisExposerLePat()
    {
        var areaPath = await CreerEquipeAsync();
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var request = new AzureDevOpsConfigRequest("mon-organisation", "MonProjet", "pat-secret-1234");
        var response = await _client.PutAsJsonAsync($"/api/equipes/{areaPath}/azure-devops-config", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("pat-secret-1234", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScrumMasterDbContext>();
        var configuration = await db.ConfigurationsAzureDevOps.FindAsync(areaPath);
        Assert.NotNull(configuration);
        Assert.DoesNotContain("pat-secret-1234", configuration!.PatChiffre);
    }

    [Fact]
    public async Task Configurer_AvecUnPatInvalide_EstRefuseSansExposerLePat()
    {
        var areaPath = await CreerEquipeAsync();
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var request = new AzureDevOpsConfigRequest("mon-organisation", "MonProjet", "pat-invalide");
        var response = await _client.PutAsJsonAsync($"/api/equipes/{areaPath}/azure-devops-config", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("pat-invalide", body);
    }

    [Fact]
    public async Task Configurer_RemplaceUneConfigurationExistante()
    {
        var areaPath = await CreerEquipeAsync();
        _factory.AzureDevOpsHandler.Repondre = _ => new HttpResponseMessage(HttpStatusCode.OK);

        await _client.PutAsJsonAsync(
            $"/api/equipes/{areaPath}/azure-devops-config",
            new AzureDevOpsConfigRequest("premiere-org", "PremierProjet", "premier-pat")
        );
        await _client.PutAsJsonAsync(
            $"/api/equipes/{areaPath}/azure-devops-config",
            new AzureDevOpsConfigRequest("seconde-org", "SecondProjet", "second-pat")
        );

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScrumMasterDbContext>();
        var configuration = await db.ConfigurationsAzureDevOps.FindAsync(areaPath);
        Assert.Equal("seconde-org", configuration!.Organisation);
        Assert.Equal("SecondProjet", configuration.Projet);
    }

    private async Task<string> CreerEquipeAsync()
    {
        var request = new CreateBoardRequest("Krypton", "Sprint-1", null, null, null, "Alex");
        var response = await _client.PostAsJsonAsync("/api/boards", request);
        response.EnsureSuccessStatusCode();
        return "Krypton";
    }
}
