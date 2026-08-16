using Microsoft.Bot.Builder;
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

public class PollClosureTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder());

    private async Task<(ScrumMasterDbContext Db, TestAdapter Adapter, RetroPollBot Bot, Guid PollId)> CreateOpenPollAsync()
    {
        var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("sonder mêlée").AssertReply(_ => { }).StartTestAsync();

        var poll = await db.PollsUtilite.SingleAsync();
        return (db, adapter, bot, poll.Id);
    }

    [Fact]
    public async Task Clore_SansPollOuvert_EstRefuse()
    {
        await using var db = CreateDb();
        var bot = CreateBot(db);
        var adapter = new TestAdapter();
        db.Equipes.Add(new Equipe { AreaPath = "Krypton", TeamsChannelId = adapter.Conversation.Conversation.Id });
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("clore mêlée")
            .AssertReply(activity => Assert.Contains("aucun poll", activity.AsMessageActivity()!.Text, StringComparison.OrdinalIgnoreCase))
            .StartTestAsync();
    }

    [Fact]
    public async Task Clore_AvecAuMoinsUnVoteUtile_DonneReunionMaintenue()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();
        db.VotesUtilite.Add(
            new VoteUtilite
            {
                PollId = pollId,
                TeamsUserId = "user1",
                NomAffiche = "Alex",
                Reponse = ReponseVote.PasNecessaire,
                DateVote = DateTimeOffset.UtcNow,
            }
        );
        db.VotesUtilite.Add(
            new VoteUtilite
            {
                PollId = pollId,
                TeamsUserId = "user2",
                NomAffiche = "Sam",
                Reponse = ReponseVote.Utile,
                DateVote = DateTimeOffset.UtcNow,
            }
        );
        await db.SaveChangesAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("clore mêlée")
            .AssertReply(activity => Assert.NotEmpty(activity.AsMessageActivity()!.Attachments))
            .StartTestAsync();

        var poll = await db.PollsUtilite.SingleAsync();
        Assert.Equal(StatutPoll.Cloture, poll.Statut);
        Assert.NotNull(poll.DateCloture);
    }

    [Fact]
    public async Task Clore_AvecUniquementDesVotesPasNecessaire_DonneReunionNonNecessaire()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();
        db.VotesUtilite.Add(
            new VoteUtilite
            {
                PollId = pollId,
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
    }

    [Fact]
    public async Task Clore_SansAucunVote_DonneReunionMaintenueParDefaut()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("clore mêlée")
            .AssertReply(activity => Assert.NotEmpty(activity.AsMessageActivity()!.Attachments))
            .StartTestAsync();

        var poll = await db.PollsUtilite.SingleAsync();
        Assert.Equal(StatutPoll.Cloture, poll.Statut);
    }

    [Fact]
    public async Task Clore_PuisVoter_EstRefuse()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();

        await new TestFlow(adapter, bot.OnTurnAsync).Send("clore mêlée").AssertReply(_ => { }).StartTestAsync();

        var voteActivity = new Activity
        {
            Type = ActivityTypes.Invoke,
            Name = "adaptiveCard/action",
            Value = Newtonsoft.Json.Linq.JObject.FromObject(
                new
                {
                    action = new
                    {
                        type = "Action.Execute",
                        verb = "vote",
                        data = new { action = "vote", pollId, reponse = "Utile" },
                    },
                }
            ),
            From = new ChannelAccount { Id = "user1", Name = "Alex" },
            Recipient = new ChannelAccount { Id = "bot" },
            Conversation = adapter.Conversation.Conversation,
            ChannelId = "test",
            ServiceUrl = "http://test.com",
        };

        await adapter.ProcessActivityAsync(voteActivity, bot.OnTurnAsync, CancellationToken.None);

        Assert.Empty(await db.VotesUtilite.ToListAsync());
    }
}
