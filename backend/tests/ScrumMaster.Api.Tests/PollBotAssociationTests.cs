using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Bots;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class PollBotAssociationTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder());

    [Fact]
    public async Task Associer_AvecEquipeExistante_MetAJourLeChannelDuMessageCourant()
    {
        await using var db = CreateDb();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton" });
        await db.SaveChangesAsync();

        var bot = CreateBot(db);
        var adapter = new TestAdapter();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("associer Krypton")
            .AssertReply(activity => Assert.Contains("Krypton", ((Activity)activity).Text))
            .StartTestAsync();

        var equipe = await db.Equipes.FirstAsync(e => e.AreaPath == "Krypton");
        Assert.False(string.IsNullOrEmpty(equipe.TeamsChannelId));
    }

    [Fact]
    public async Task Associer_AvecAreaPathInconnue_EstRefuseSansCreerDeChannel()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("associer Inconnue")
            .AssertReply(activity => Assert.Contains("introuvable", ((Activity)activity).Text))
            .StartTestAsync();

        Assert.False(await db.Equipes.AnyAsync(e => e.AreaPath == "Inconnue"));
    }

    [Fact]
    public async Task CommandeNonReconnue_RecoitUnMessageDAide()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("bonjour")
            .AssertReply(activity => Assert.Contains("non reconnue", ((Activity)activity).Text))
            .StartTestAsync();
    }
}
