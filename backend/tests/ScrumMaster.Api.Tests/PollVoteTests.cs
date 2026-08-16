using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ScrumMaster.Api.Bots;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;
using Xunit;

namespace ScrumMaster.Api.Tests;

public class PollVoteTests
{
    private static ScrumMasterDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScrumMasterDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ScrumMasterDbContext(options);
    }

    private static RetroPollBot CreateBot(ScrumMasterDbContext db) => new(new PollService(db), new PollCardBuilder());

    private static Activity CreateVoteInvokeActivity(Guid pollId, string reponse, string userId, string userName) =>
        new()
        {
            Type = ActivityTypes.Invoke,
            Name = "adaptiveCard/action",
            Value = JObject.FromObject(
                new
                {
                    action = new
                    {
                        type = "Action.Execute",
                        verb = "vote",
                        data = new { action = "vote", pollId, reponse },
                    },
                }
            ),
            From = new ChannelAccount { Id = userId, Name = userName },
            Recipient = new ChannelAccount { Id = "bot" },
            Conversation = new ConversationAccount { Id = "Convo1" },
            ChannelId = "test",
            ServiceUrl = "http://test.com",
        };

    private static InvokeResponse GetInvokeResponse(TestAdapter adapter) => (InvokeResponse)adapter.ActiveQueue.Dequeue().Value;

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
    public async Task Vote_SurUnPollOuvert_EstEnregistreEtRetourneLaCarteMiseAJour()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();

        await adapter.ProcessActivityAsync(CreateVoteInvokeActivity(pollId, "Utile", "user1", "Alex"), bot.OnTurnAsync, CancellationToken.None);

        var response = GetInvokeResponse(adapter);
        Assert.Equal(200, response.Status);

        var vote = await db.VotesUtilite.SingleAsync();
        Assert.Equal("user1", vote.TeamsUserId);
        Assert.Equal(ReponseVote.Utile, vote.Reponse);
    }

    [Fact]
    public async Task Vote_DeuxFoisParLeMemeMembre_RemplaceLeVotePrecedent()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();

        await adapter.ProcessActivityAsync(CreateVoteInvokeActivity(pollId, "Utile", "user1", "Alex"), bot.OnTurnAsync, CancellationToken.None);
        await adapter.ProcessActivityAsync(
            CreateVoteInvokeActivity(pollId, "PasNecessaire", "user1", "Alex"),
            bot.OnTurnAsync,
            CancellationToken.None
        );

        var votes = await db.VotesUtilite.Where(v => v.PollId == pollId).ToListAsync();
        Assert.Single(votes);
        Assert.Equal(ReponseVote.PasNecessaire, votes[0].Reponse);
    }

    [Fact]
    public async Task Vote_SurUnPollClos_EstRefuse()
    {
        var (db, adapter, bot, pollId) = await CreateOpenPollAsync();
        var poll = await db.PollsUtilite.SingleAsync();
        poll.Statut = StatutPoll.Cloture;
        await db.SaveChangesAsync();

        await adapter.ProcessActivityAsync(CreateVoteInvokeActivity(pollId, "Utile", "user1", "Alex"), bot.OnTurnAsync, CancellationToken.None);

        var response = GetInvokeResponse(adapter);
        var cardResponse = Assert.IsType<AdaptiveCardInvokeResponse>(response.Body);
        Assert.Equal(400, cardResponse.StatusCode);
        Assert.Empty(await db.VotesUtilite.ToListAsync());
    }
}
