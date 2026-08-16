using Microsoft.EntityFrameworkCore;
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

app.Run();

public partial class Program { }
