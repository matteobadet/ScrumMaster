using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.AzureDevOps;
using ScrumMaster.Api.Bots;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Hubs;
using ScrumMaster.Api.Middleware;
using ScrumMaster.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ScrumMasterDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ScrumMasterDb")));

builder.Services.AddSignalR();

builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<PostItService>();
builder.Services.AddScoped<ParticipantService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<PollService>();
builder.Services.AddScoped<RappelService>();
builder.Services.AddSingleton<PollCardBuilder>();

// Système d'extensions/étapes (specs/006-systeme-extensions-etapes)
builder.Services.AddScoped<EtapeService>();
builder.Services.AddScoped<MiniJeuService>();
builder.Services.AddScoped<PollPersonnaliseService>();

// Azure DevOps (specs/005-azure-devops-boards) — le PAT est chiffré via Data Protection avant
// stockage ; l'anneau de clés est persisté dans PostgreSQL (research.md#2) pour survivre aux
// redémarrages de pod et être partagé entre réplicas sur k3s.
builder.Services.AddDataProtection().PersistKeysToDbContext<ScrumMasterDbContext>();
builder.Services.AddHttpClient<AzureDevOpsClient>();
builder.Services.AddScoped<AzureDevOpsConfigService>();
builder.Services.AddScoped<AzureDevOpsBoardService>();

// Bot Framework (specs/002-poll-utilite-reunion) — identifiants via
// MicrosoftAppId/MicrosoftAppPassword/MicrosoftAppTenantId (appsettings / Secret Kubernetes).
// CloudAdapter construit sa propre ConfigurationBotFrameworkAuthentication à partir de
// IConfiguration : ne pas enregistrer BotFrameworkAuthentication séparément, sous peine de
// rendre ses deux constructeurs éligibles et donc ambigus pour le conteneur DI.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, CloudAdapter>();
builder.Services.AddTransient<IBot, RetroPollBot>();

const string FrontendDevCorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendDevCorsPolicy,
        policy =>
            policy
                .WithOrigins("http://localhost:5173", "http://localhost:5174")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
    );
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScrumMasterDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        // Fournisseur non relationnel (ex. InMemory en tests) : les migrations ne s'appliquent pas.
        await db.Database.EnsureCreatedAsync();
    }

    await ThemeSeeder.EnsureSeededAsync(db);
    await MiniJeuSeeder.EnsureSeededAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(FrontendDevCorsPolicy);
}

app.UseMiddleware<DomainExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<RetroBoardHub>("/hubs/retro-board");
app.MapPost(
    "/api/messages",
    async (HttpRequest request, HttpResponse response, IBotFrameworkHttpAdapter adapter, IBot bot, CancellationToken cancellationToken) =>
    {
        await adapter.ProcessAsync(request, response, bot, cancellationToken);
    }
);

app.Run();

public partial class Program { }
