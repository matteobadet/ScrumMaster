using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record VoteResult(Guid PostItId, int NombreVotes, int VotesRestants);

/// <summary>
/// Quota de votes révisé : compté par étape, pas cumulé sur tout le board
/// (specs/006-systeme-extensions-etapes, data-model.md).
/// </summary>
public class VoteService(ScrumMasterDbContext db)
{
    public async Task<VoteResult> VoteAsync(Guid boardId, Guid postItId, Guid participantId)
    {
        var (board, etape) = await GetBoardAndEtapePourPostItAsync(boardId, postItId);
        EnsureEtapeActive(etape);

        var dejaVote = await db.Votes.AnyAsync(v => v.PostItId == postItId && v.ParticipantId == participantId);
        if (dejaVote)
        {
            throw new DomainValidationException("Vous avez déjà voté pour ce post-it.");
        }

        var votesUtilises = await CountVotesUtilisesAsync(etape.Id, participantId);
        if (votesUtilises >= board.MaxVotesParParticipant)
        {
            throw new DomainValidationException($"Vous avez atteint votre limite de {board.MaxVotesParParticipant} votes sur cette étape.");
        }

        db.Votes.Add(new Vote { PostItId = postItId, ParticipantId = participantId });
        await db.SaveChangesAsync();

        return await BuildResultAsync(board.MaxVotesParParticipant, etape.Id, postItId, participantId);
    }

    public async Task<VoteResult> RemoveVoteAsync(Guid boardId, Guid postItId, Guid participantId)
    {
        var (board, etape) = await GetBoardAndEtapePourPostItAsync(boardId, postItId);
        EnsureEtapeActive(etape);

        var vote = await db.Votes.FirstOrDefaultAsync(v => v.PostItId == postItId && v.ParticipantId == participantId);
        if (vote is null)
        {
            throw new DomainValidationException("Aucun vote à retirer pour ce post-it.");
        }

        db.Votes.Remove(vote);
        await db.SaveChangesAsync();

        return await BuildResultAsync(board.MaxVotesParParticipant, etape.Id, postItId, participantId);
    }

    /// <summary>Votes restants du participant pour l'étape "Colonnes et post-its" active du board, s'il y en a une.</summary>
    public async Task<int> GetVotesRestantsAsync(Guid boardId, Guid participantId)
    {
        var board = await db.Boards.Include(b => b.Etapes).FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var etapeActive = board.Etapes.FirstOrDefault(e => e.Statut == StatutEtape.Active && e.Type == TypeEtape.ColonnesEtPostIts);
        if (etapeActive is null)
        {
            return board.MaxVotesParParticipant;
        }

        var votesUtilises = await CountVotesUtilisesAsync(etapeActive.Id, participantId);
        return Math.Max(0, board.MaxVotesParParticipant - votesUtilises);
    }

    private async Task<VoteResult> BuildResultAsync(int maxVotesParParticipant, Guid etapeId, Guid postItId, Guid participantId)
    {
        var nombreVotes = await db.Votes.CountAsync(v => v.PostItId == postItId);
        var votesUtilises = await CountVotesUtilisesAsync(etapeId, participantId);
        return new VoteResult(postItId, nombreVotes, Math.Max(0, maxVotesParParticipant - votesUtilises));
    }

    private Task<int> CountVotesUtilisesAsync(Guid etapeId, Guid participantId) =>
        db.Votes.CountAsync(v => v.ParticipantId == participantId && v.PostIt!.EtapeId == etapeId);

    private async Task<(Board Board, Etape Etape)> GetBoardAndEtapePourPostItAsync(Guid boardId, Guid postItId)
    {
        var postIt = await db.PostIts.Include(p => p.Etape!).FirstOrDefaultAsync(p => p.Id == postItId);
        if (postIt is null || postIt.Etape!.BoardId != boardId)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        return (board, postIt.Etape);
    }

    private static void EnsureEtapeActive(Etape etape)
    {
        if (etape.Statut != StatutEtape.Active)
        {
            throw new DomainValidationException("Cette étape n'est plus active.");
        }
    }
}
