using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Bots;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class RappelManuelTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder(), new RappelService(db));

    [Fact]
    public async Task Rappeler_SurChannelAssocie_EnvoieUnRappelSansPoll()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("rappeler mêlée")
            .AssertReply(activity => Assert.Contains("Rappel", activity.AsMessageActivity()!.Text))
            .StartTestAsync();

        Assert.Empty(await db.PollsUtilite.ToListAsync());
        Assert.Single(await db.RappelsEnvoyes.ToListAsync());
    }

    [Fact]
    public async Task Rappeler_SurChannelNonAssocie_EstRefuse()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("rappeler mêlée")
            .AssertReply(activity => Assert.Contains("associé à aucune équipe", activity.AsMessageActivity()!.Text))
            .StartTestAsync();

        Assert.Empty(await db.RappelsEnvoyes.ToListAsync());
    }

    [Fact]
    public async Task Rappeler_DeuxFoisLeMemeJour_EstRefuseLaSecondeFois()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("rappeler mêlée").AssertReply(_ => { }).StartTestAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("rappeler mêlée")
            .AssertReply(activity => Assert.Contains("déjà été envoyé", activity.AsMessageActivity()!.Text))
            .StartTestAsync();

        Assert.Single(await db.RappelsEnvoyes.ToListAsync());
    }
}
