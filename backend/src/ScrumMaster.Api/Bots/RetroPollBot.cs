using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using ScrumMaster.Api.Cards;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Bots;

/// <summary>
/// Bot Teams du poll d'utilité de réunion — voir specs/002-poll-utilite-reunion/contracts/.
/// Les commandes textuelles (associer/sonder/clore) sont traitées dans le tour de conversation
/// courant, sans messagerie proactive (research.md#1). Le vote se fait via une Adaptive Card
/// Action.Execute (research.md#2), traitée par OnAdaptiveCardInvokeAsync.
/// </summary>
public class RetroPollBot(PollService pollService, PollCardBuilder cardBuilder) : ActivityHandler
{
    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var texte = (turnContext.Activity.RemoveRecipientMention() ?? string.Empty).Trim();
        var mots = texte.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (mots.Length >= 2 && mots[0].Equals("associer", StringComparison.OrdinalIgnoreCase))
        {
            var areaPath = string.Join(' ', mots.Skip(1));
            await TraiterAssocierAsync(turnContext, areaPath, cancellationToken);
            return;
        }

        if (mots.Length >= 2 && mots[0].Equals("sonder", StringComparison.OrdinalIgnoreCase))
        {
            await TraiterSonderAsync(turnContext, mots[1], cancellationToken);
            return;
        }

        if (mots.Length >= 2 && mots[0].Equals("clore", StringComparison.OrdinalIgnoreCase))
        {
            await TraiterCloreAsync(turnContext, mots[1], cancellationToken);
            return;
        }

        await turnContext.SendActivityAsync(
            MessageFactory.Text(
                "Commande non reconnue. Essayez : \"associer <area-path>\", \"sonder <mêlée|rétro>\", \"clore <mêlée|rétro>\"."
            ),
            cancellationToken
        );
    }

    private async Task TraiterAssocierAsync(ITurnContext turnContext, string areaPath, CancellationToken cancellationToken)
    {
        try
        {
            await pollService.AssocierChannelAsync(areaPath, turnContext.Activity.Conversation.Id);
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"Ce channel est maintenant associé à l'équipe \"{areaPath}\"."),
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is DomainValidationException or DomainNotFoundException)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
        }
    }

    private async Task TraiterSonderAsync(ITurnContext turnContext, string typeMot, CancellationToken cancellationToken)
    {
        var typeReunion = ParserTypeReunion(typeMot);
        if (typeReunion is null)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Type de réunion non reconnu. Utilisez \"mêlée\" ou \"rétro\"."),
                cancellationToken
            );
            return;
        }

        try
        {
            var result = await pollService.DeclencherPollAsync(turnContext.Activity.Conversation.Id, typeReunion.Value);
            var carte = cardBuilder.BuildPollCard(result.PollId, result.TypeReunion, nombreUtile: 0, nombrePasNecessaire: 0);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(carte), cancellationToken);
        }
        catch (DomainValidationException ex)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
        }
    }

    private async Task TraiterCloreAsync(ITurnContext turnContext, string typeMot, CancellationToken cancellationToken)
    {
        var typeReunion = ParserTypeReunion(typeMot);
        if (typeReunion is null)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Type de réunion non reconnu. Utilisez \"mêlée\" ou \"rétro\"."),
                cancellationToken
            );
            return;
        }

        try
        {
            var result = await pollService.CloturerAsync(turnContext.Activity.Conversation.Id, typeReunion.Value);
            var carte = cardBuilder.BuildResultCard(result.TypeReunion, result.ReunionMaintenue, result.Votes);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(carte), cancellationToken);
        }
        catch (DomainValidationException ex)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
        }
    }

    protected override async Task<AdaptiveCardInvokeResponse> OnAdaptiveCardInvokeAsync(
        ITurnContext<IInvokeActivity> turnContext,
        AdaptiveCardInvokeValue invokeValue,
        CancellationToken cancellationToken
    )
    {
        var data = invokeValue.Action.Data as JObject ?? JObject.FromObject(invokeValue.Action.Data);
        var pollId = data["pollId"]!.ToObject<Guid>();
        var reponse = Enum.Parse<ReponseVote>(data.Value<string>("reponse")!);
        var teamsUserId = turnContext.Activity.From.AadObjectId ?? turnContext.Activity.From.Id;
        var nomAffiche = turnContext.Activity.From.Name ?? teamsUserId;

        try
        {
            var result = await pollService.VoterAsync(pollId, teamsUserId, nomAffiche, reponse);
            var carteMiseAJour = cardBuilder.BuildPollCardContent(result.PollId, result.TypeReunion, result.NombreUtile, result.NombrePasNecessaire);

            return new AdaptiveCardInvokeResponse
            {
                StatusCode = 200,
                Type = "application/vnd.microsoft.card.adaptive",
                Value = carteMiseAJour,
            };
        }
        catch (Exception ex) when (ex is DomainNotFoundException or DomainForbiddenException)
        {
            return new AdaptiveCardInvokeResponse
            {
                StatusCode = 400,
                Type = "application/vnd.microsoft.error",
                Value = JObject.FromObject(new { code = "BadRequest", message = ex.Message }),
            };
        }
    }

    private static TypeReunion? ParserTypeReunion(string mot) =>
        mot.ToLowerInvariant() switch
        {
            "mêlée" or "melee" => TypeReunion.Melee,
            "rétro" or "retro" or "rétrospective" or "retrospective" => TypeReunion.Retrospective,
            _ => null,
        };
}
