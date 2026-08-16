using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Bots;

/// <summary>
/// Bot Teams du poll d'utilité de réunion — voir specs/002-poll-utilite-reunion/contracts/.
/// Les commandes textuelles (associer/sonder/clore) sont traitées dans le tour de conversation
/// courant, sans messagerie proactive (research.md#1).
/// </summary>
public class RetroPollBot(PollService pollService) : ActivityHandler
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

    protected override Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
    {
        // Implémenté par User Story 2 (vote via carte adaptative).
        return Task.FromResult(new InvokeResponse { Status = 200 });
    }
}
