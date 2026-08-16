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

public class RappelAutomatiqueTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder(), new RappelService(db));

    private async Task<(ScrumMasterDbContext Db, TestAdapter Adapter, RetroPollBot Bot)> CreateAssocieeAsync()
    {
        var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();
        return (db, adapter, bot);
    }

    [Fact]
    public async Task Clore_AvecResultatMaintenue_EnvoieUnRappelApresLaCarteDeResultat()
    {
        var (db, adapter, bot) = await CreateAssocieeAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("sonder mêlée").AssertReply(_ => { }).StartTestAsync();
        var poll = await db.PollsUtilite.SingleAsync();
        db.VotesUtilite.Add(
            new VoteUtilite
            {
                PollId = poll.Id,
                TeamsUserId = "user1",
                NomAffiche = "Alex",
                Reponse = ReponseVote.Utile,
                DateVote = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("clore mêlée")
            .AssertReply(activity => Assert.NotEmpty(activity.AsMessageActivity()!.Attachments))
            .AssertReply(activity => Assert.Contains("Rappel", activity.AsMessageActivity()!.Text))
            .StartTestAsync();

        Assert.Single(await db.RappelsEnvoyes.ToListAsync());
    }

    [Fact]
    public async Task Clore_AvecResultatPasNecessaire_NEnvoieAucunRappel()
    {
        var (db, adapter, bot) = await CreateAssocieeAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("sonder mêlée").AssertReply(_ => { }).StartTestAsync();
        var poll = await db.PollsUtilite.SingleAsync();
        db.VotesUtilite.Add(
            new VoteUtilite
            {
                PollId = poll.Id,
                TeamsUserId = "user1",
                NomAffiche = "Alex",
                Reponse = ReponseVote.PasNecessaire,
                DateVote = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("clore mêlée")
            .AssertReply(activity => Assert.NotEmpty(activity.AsMessageActivity()!.Attachments))
            .StartTestAsync();

        Assert.Empty(await db.RappelsEnvoyes.ToListAsync());
    }
}
