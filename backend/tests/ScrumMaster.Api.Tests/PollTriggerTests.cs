using Microsoft.Bot.Builder.Adapters;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Bots;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class PollTriggerTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder());

    [Fact]
    public async Task Sonder_SurChannelNonAssocie_EstRefuse()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("sonder mêlée")
            .AssertReply(activity => Assert.Contains("associé à aucune équipe", activity.AsMessageActivity()!.Text))
            .StartTestAsync();
    }

    [Fact]
    public async Task Sonder_SurChannelAssocie_CreeUnPollEtEnvoieLaCarte()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("sonder mêlée")
            .AssertReply(activity => Assert.NotEmpty(activity.AsMessageActivity()!.Attachments))
            .StartTestAsync();

        var poll = await db.PollsUtilite.SingleAsync();
        Assert.Equal("Krypton", poll.AreaPath);
        Assert.Equal(TypeReunion.Melee, poll.TypeReunion);
        Assert.Equal(StatutPoll.Ouvert, poll.Statut);
    }

    [Fact]
    public async Task Sonder_AvecUnPollDejaOuvert_EstRefuse()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("sonder mêlée").AssertReply(_ => { }).StartTestAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("sonder mêlée")
            .AssertReply(activity => Assert.Contains("déjà ouvert", activity.AsMessageActivity()!.Text))
            .StartTestAsync();

        Assert.Equal(1, await db.PollsUtilite.CountAsync());
    }
}
