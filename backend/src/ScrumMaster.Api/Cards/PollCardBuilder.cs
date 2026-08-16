using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScrumMaster.Api.Models;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Cards;

/// <summary>
/// Construit les Adaptive Cards du poll d'utilité — voir
/// specs/002-poll-utilite-reunion/contracts/adaptive-cards.md.
/// </summary>
public class PollCardBuilder
{
    public Attachment BuildPollCard(Guid pollId, TypeReunion typeReunion, int nombreUtile, int nombrePasNecessaire) =>
        ToAttachment(BuildPollCardObject(pollId, typeReunion, nombreUtile, nombrePasNecessaire));

    /// <summary>Contenu JSON de la carte de poll — utilisé pour la réponse Invoke (Action.Execute) qui met à jour la carte en place.</summary>
    public JObject BuildPollCardContent(Guid pollId, TypeReunion typeReunion, int nombreUtile, int nombrePasNecessaire) =>
        JObject.Parse(BuildPollCardObject(pollId, typeReunion, nombreUtile, nombrePasNecessaire).ToJson());

    private static AdaptiveCard BuildPollCardObject(Guid pollId, TypeReunion typeReunion, int nombreUtile, int nombrePasNecessaire) =>
        new(new AdaptiveSchemaVersion(1, 4))
        {
            Body =
            {
                new AdaptiveTextBlock($"{Libelle(typeReunion)} du jour — utile ?")
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                },
                new AdaptiveTextBlock($"{nombreUtile} Utile · {nombrePasNecessaire} Pas nécessaire") { Wrap = true },
            },
            Actions =
            {
                CreateVoteAction("Utile", pollId, ReponseVote.Utile),
                CreateVoteAction("Pas nécessaire", pollId, ReponseVote.PasNecessaire),
            },
        };

    private static AdaptiveExecuteAction CreateVoteAction(string titre, Guid pollId, ReponseVote reponse) =>
        new()
        {
            Title = titre,
            Verb = "vote",
            Data = JObject.FromObject(new { action = "vote", pollId, reponse = reponse.ToString() }),
        };

    public Attachment BuildResultCard(TypeReunion typeReunion, bool reunionMaintenue, IReadOnlyList<VoteDetail> votes) =>
        ToAttachment(BuildResultCardObject(typeReunion, reunionMaintenue, votes));

    private static AdaptiveCard BuildResultCardObject(TypeReunion typeReunion, bool reunionMaintenue, IReadOnlyList<VoteDetail> votes)
    {
        var resultat = reunionMaintenue ? "maintenue" : "pas nécessaire";
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4))
        {
            Body =
            {
                new AdaptiveTextBlock($"{Libelle(typeReunion)} du jour : {resultat}")
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                },
            },
        };

        if (votes.Count == 0)
        {
            card.Body.Add(new AdaptiveTextBlock("Aucun vote reçu.") { Wrap = true });
        }
        else
        {
            foreach (var vote in votes)
            {
                var reponse = vote.Reponse == ReponseVote.Utile ? "Utile" : "Pas nécessaire";
                card.Body.Add(new AdaptiveTextBlock($"{vote.NomAffiche} : {reponse}") { Wrap = true });
            }
        }

        return card;
    }

    private static Attachment ToAttachment(AdaptiveCard card) =>
        new() { ContentType = AdaptiveCard.ContentType, Content = JsonConvert.DeserializeObject(card.ToJson()) };

    private static string Libelle(TypeReunion typeReunion) =>
        typeReunion == TypeReunion.Melee ? "Mêlée" : "Rétrospective";
}
