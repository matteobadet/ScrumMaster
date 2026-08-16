using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScrumMaster.Api.Data;

namespace ScrumMaster.Api.Tests;

/// <summary>
/// Remplace la persistance PostgreSQL par le fournisseur EF Core InMemory (une base isolée par
/// instance) pour exécuter les tests d'intégration sans dépendance à un serveur Postgres réel.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"scrummaster-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ScrumMasterDbContext>>();
            services.AddDbContext<ScrumMasterDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
