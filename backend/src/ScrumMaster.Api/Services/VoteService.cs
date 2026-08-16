using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record VoteResult(Guid PostItId, int NombreVotes, int VotesRestants);

public class VoteService(ScrumMasterDbContext db)
{
    public async Task<VoteResult> VoteAsync(Guid boardId, Guid postItId, Guid participantId)
    {
        var board = await GetBoardAsync(boardId);
        BoardClosureGuard.EnsureActif(board);
        await EnsurePostItExistsAsync(boardId, postItId);

        var dejaVote = await db.Votes.AnyAsync(v => v.PostItId == postItId && v.ParticipantId == participantId);
        if (dejaVote)
        {
            throw new DomainValidationException("Vous avez déjà voté pour ce post-it.");
        }

        var votesUtilises = await CountVotesUtilisesAsync(boardId, participantId);
        if (votesUtilises >= board.MaxVotesParParticipant)
        {
            throw new DomainValidationException(
                $"Vous avez atteint votre limite de {board.MaxVotesParParticipant} votes sur ce board."
            );
        }

        db.Votes.Add(new Vote { PostItId = postItId, ParticipantId = participantId });
        await db.SaveChangesAsync();

        return await BuildResultAsync(board, postItId, participantId);
    }

    public async Task<VoteResult> RemoveVoteAsync(Guid boardId, Guid postItId, Guid participantId)
    {
        var board = await GetBoardAsync(boardId);
        BoardClosureGuard.EnsureActif(board);
        await EnsurePostItExistsAsync(boardId, postItId);

        var vote = await db.Votes.FirstOrDefaultAsync(v => v.PostItId == postItId && v.ParticipantId == participantId);
        if (vote is null)
        {
            throw new DomainValidationException("Aucun vote à retirer pour ce post-it.");
        }

        db.Votes.Remove(vote);
        await db.SaveChangesAsync();

        return await BuildResultAsync(board, postItId, participantId);
    }

    public async Task<int> GetVotesRestantsAsync(Guid boardId, Guid participantId)
    {
        var board = await GetBoardAsync(boardId);
        var votesUtilises = await CountVotesUtilisesAsync(boardId, participantId);
        return Math.Max(0, board.MaxVotesParParticipant - votesUtilises);
    }

    private async Task<VoteResult> BuildResultAsync(Board board, Guid postItId, Guid participantId)
    {
        var nombreVotes = await db.Votes.CountAsync(v => v.PostItId == postItId);
        var votesUtilises = await CountVotesUtilisesAsync(board.Id, participantId);
        return new VoteResult(postItId, nombreVotes, Math.Max(0, board.MaxVotesParParticipant - votesUtilises));
    }

    private Task<int> CountVotesUtilisesAsync(Guid boardId, Guid participantId) =>
        db.Votes.CountAsync(v => v.ParticipantId == participantId && v.PostIt!.BoardId == boardId);

    private async Task<Board> GetBoardAsync(Guid boardId)
    {
        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        return board;
    }

    private async Task EnsurePostItExistsAsync(Guid boardId, Guid postItId)
    {
        var exists = await db.PostIts.AnyAsync(p => p.Id == postItId && p.BoardId == boardId);
        if (!exists)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }
    }
}
