using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Hubs;

/// <summary>
/// Hub temps réel du board de rétrospective — un groupe SignalR par BoardId.
/// Voir specs/001-retro-board-base/contracts/realtime-hub.md pour le contrat complet.
/// </summary>
public class RetroBoardHub(
    ScrumMasterDbContext db,
    PostItService postItService,
    VoteService voteService,
    BoardService boardService,
    AzureDevOpsBoardService azureDevOpsBoardService,
    EtapeService etapeService,
    MiniJeuService miniJeuService,
    PollPersonnaliseService pollPersonnaliseService
) : Hub
{
    public async Task JoinBoard(Guid boardId, Guid participantId)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.BoardId == boardId);
        if (participant is null)
        {
            throw new HubException("Participant introuvable sur ce board.");
        }

        participant.ConnectionId = Context.ConnectionId;
        await db.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, boardId.ToString());

        var votesRestants = await voteService.GetVotesRestantsAsync(boardId, participantId);

        await Clients
            .Group(boardId.ToString())
            .SendAsync(
                "ParticipantJoined",
                new
                {
                    participantId = participant.Id,
                    nomAffiche = participant.NomAffiche,
                    role = participant.Role.ToString(),
                    votesRestants,
                }
            );
    }

    public async Task Vote(Guid boardId, Guid postItId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => voteService.VoteAsync(boardId, postItId, callerId));

        await Clients
            .Group(boardId.ToString())
            .SendAsync("VoteChanged", new { postItId = result.PostItId, nombreVotes = result.NombreVotes });
        await Clients
            .Caller.SendAsync(
                "MonVoteChanged",
                new
                {
                    postItId = result.PostItId,
                    voteDuParticipant = true,
                    votesRestants = result.VotesRestants,
                }
            );
    }

    public async Task RemoveVote(Guid boardId, Guid postItId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => voteService.RemoveVoteAsync(boardId, postItId, callerId));

        await Clients
            .Group(boardId.ToString())
            .SendAsync("VoteChanged", new { postItId = result.PostItId, nombreVotes = result.NombreVotes });
        await Clients
            .Caller.SendAsync(
                "MonVoteChanged",
                new
                {
                    postItId = result.PostItId,
                    voteDuParticipant = false,
                    votesRestants = result.VotesRestants,
                }
            );
    }

    public async Task ChangeTheme(Guid boardId, Guid? themeId, ThemePersonnaliseDto? themePersonnalise)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(
            () => boardService.ChangeThemeAsync(boardId, callerId, themeId, themePersonnalise)
        );

        await Clients
            .Group(boardId.ToString())
            .SendAsync(
                "ThemeChanged",
                new
                {
                    theme = new
                    {
                        id = result.ThemeId,
                        nom = result.Nom,
                        icone = result.Icone,
                        contexte = result.Contexte,
                    },
                    colonnes = result.Colonnes,
                }
            );
    }

    public async Task AvancerEtape(Guid boardId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => etapeService.AvancerEtapeAsync(boardId, callerId));

        if (result.BoardFerme)
        {
            await Clients.Group(boardId.ToString()).SendAsync("BoardClosed", new { boardId });
        }
        else
        {
            await Clients
                .Group(boardId.ToString())
                .SendAsync("EtapeChangee", new { nouvelleEtapeId = result.NouvelleEtapeId });
        }
    }

    public async Task RepondreMiniJeu(Guid boardId, Guid etapeId, string reponse)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => miniJeuService.RepondreAsync(boardId, etapeId, callerId, reponse));

        await Clients
            .Group(boardId.ToString())
            .SendAsync(
                "ReponseMiniJeuChangee",
                new
                {
                    etapeId,
                    participantId = result.ParticipantId,
                    nomAffiche = result.NomAffiche,
                    reponse = result.Reponse,
                }
            );
    }

    public async Task ProposerLettrePendu(Guid boardId, Guid etapeId, string lettre)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => miniJeuService.ProposerLettrePenduAsync(boardId, etapeId, callerId, lettre));
        if (result is null)
        {
            return;
        }

        await Clients
            .Group(boardId.ToString())
            .SendAsync(
                "LettrePenduProposee",
                new
                {
                    etapeId,
                    lettre = result.Lettre,
                    correcte = result.Correcte,
                    motMasquePendu = result.MotMasque,
                    lettresProposeesPendu = result.LettresProposees,
                    essaisRestantsPendu = result.EssaisRestants,
                    maxEssaisPendu = result.MaxEssais,
                    etatPendu = result.Etat,
                    motCompletPendu = result.MotComplet,
                }
            );
    }

    public async Task DefinirLienExterne(Guid boardId, Guid etapeId, string nom, string url)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => miniJeuService.DefinirLienExterneAsync(boardId, etapeId, callerId, nom, url));

        await Clients
            .Group(boardId.ToString())
            .SendAsync("LienExterneDefini", new { etapeId, nom = result.Nom, url = result.Url });
    }

    public async Task RepondrePollPersonnalise(Guid boardId, Guid etapeId, Guid optionId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(
            () => pollPersonnaliseService.RepondreAsync(boardId, etapeId, callerId, optionId)
        );

        await Clients
            .Group(boardId.ToString())
            .SendAsync("ReponsePollPersonnaliseChangee", new { etapeId, decompteParOption = result.DecompteParOption });
    }

    public async Task AddPostIt(Guid boardId, Guid colonneId, string texte)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => postItService.AddAsync(boardId, colonneId, texte, callerId));

        await Clients
            .Group(boardId.ToString())
            .SendAsync(
                "PostItAdded",
                new
                {
                    postIt = new
                    {
                        id = result.Id,
                        colonneId = result.ColonneId,
                        texte = result.Texte,
                        auteur = result.Auteur,
                        auteurParticipantId = result.AuteurParticipantId,
                        nombreVotes = 0,
                        workItemExporteId = (int?)null,
                    },
                }
            );
    }

    public async Task MovePostIt(Guid boardId, Guid postItId, Guid colonneId)
    {
        await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => postItService.MoveAsync(boardId, postItId, colonneId));

        await Clients
            .Group(boardId.ToString())
            .SendAsync("PostItMoved", new { postItId = result.Id, colonneId = result.ColonneId });
    }

    public async Task EditPostIt(Guid boardId, Guid postItId, string texte)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => postItService.EditAsync(boardId, postItId, texte, callerId));

        await Clients.Group(boardId.ToString()).SendAsync("PostItUpdated", new { postItId = result.Id, texte = result.Texte });
    }

    public async Task DeletePostIt(Guid boardId, Guid postItId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        await RunOrThrowHubExceptionAsync(() => postItService.DeleteAsync(boardId, postItId, callerId));

        await Clients.Group(boardId.ToString()).SendAsync("PostItDeleted", new { postItId });
    }

    public async Task ImportWorkItems(Guid boardId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var importes = await RunOrThrowHubExceptionAsync(() => azureDevOpsBoardService.ImporterWorkItemsAsync(boardId, callerId));

        foreach (var postIt in importes)
        {
            await Clients
                .Group(boardId.ToString())
                .SendAsync(
                    "PostItAdded",
                    new
                    {
                        postIt = new
                        {
                            id = postIt.Id,
                            colonneId = postIt.ColonneId,
                            texte = postIt.Texte,
                            auteur = postIt.Auteur,
                            auteurParticipantId = postIt.AuteurParticipantId,
                            nombreVotes = 0,
                            workItemExporteId = (int?)null,
                        },
                    }
                );
        }
    }

    public async Task ExportPostIt(Guid boardId, Guid postItId)
    {
        var callerId = await ResolveCallerParticipantIdAsync(boardId);

        var result = await RunOrThrowHubExceptionAsync(() => azureDevOpsBoardService.ExporterPostItAsync(boardId, callerId, postItId));

        await Clients
            .Group(boardId.ToString())
            .SendAsync("PostItExported", new { postItId = result.PostItId, workItemId = result.WorkItemId });
    }

    private async Task<Guid> ResolveCallerParticipantIdAsync(Guid boardId)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p =>
            p.ConnectionId == Context.ConnectionId && p.BoardId == boardId
        );
        if (participant is null)
        {
            throw new HubException("Vous devez rejoindre le board (JoinBoard) avant d'interagir avec lui.");
        }

        return participant.Id;
    }

    private static async Task<T> RunOrThrowHubExceptionAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
            when (ex is DomainValidationException or DomainNotFoundException or DomainForbiddenException or DomainUpstreamException)
        {
            throw new HubException(ex.Message);
        }
    }

    private static async Task RunOrThrowHubExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
            when (ex is DomainValidationException or DomainNotFoundException or DomainForbiddenException or DomainUpstreamException)
        {
            throw new HubException(ex.Message);
        }
    }
}
