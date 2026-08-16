using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record PostItResult(Guid Id, Guid ColonneId, string Texte, string Auteur, Guid AuteurParticipantId);

public class PostItService(ScrumMasterDbContext db)
{
    public async Task<PostItResult> AddAsync(Guid boardId, Guid colonneId, string texte, Guid auteurParticipantId)
    {
        ValidateTexte(texte);

        var board = await GetBoardWithThemeAsync(boardId);
        ValidateColonneAppartientAuBoard(board, colonneId);

        var auteur = await db.Participants.FirstOrDefaultAsync(p => p.Id == auteurParticipantId && p.BoardId == boardId);
        if (auteur is null)
        {
            throw new DomainNotFoundException($"Participant {auteurParticipantId} introuvable sur ce board.");
        }

        var now = DateTimeOffset.UtcNow;
        var postIt = new PostIt
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            ColonneId = colonneId,
            Texte = texte.Trim(),
            AuteurParticipantId = auteurParticipantId,
            DateCreation = now,
            DateModification = now,
        };
        db.PostIts.Add(postIt);
        await db.SaveChangesAsync();

        return new PostItResult(postIt.Id, postIt.ColonneId, postIt.Texte, auteur.NomAffiche, auteur.Id);
    }

    public async Task<PostItResult> EditAsync(Guid boardId, Guid postItId, string texte, Guid callerParticipantId)
    {
        ValidateTexte(texte);

        var postIt = await GetOwnedPostItAsync(boardId, postItId, callerParticipantId);
        postIt.Texte = texte.Trim();
        postIt.DateModification = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var auteur = await db.Participants.FirstAsync(p => p.Id == postIt.AuteurParticipantId);
        return new PostItResult(postIt.Id, postIt.ColonneId, postIt.Texte, auteur.NomAffiche, auteur.Id);
    }

    public async Task<PostItResult> MoveAsync(Guid boardId, Guid postItId, Guid colonneId)
    {
        var board = await GetBoardWithThemeAsync(boardId);
        ValidateColonneAppartientAuBoard(board, colonneId);

        var postIt = await db.PostIts.FirstOrDefaultAsync(p => p.Id == postItId && p.BoardId == boardId);
        if (postIt is null)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        postIt.ColonneId = colonneId;
        postIt.DateModification = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var auteur = await db.Participants.FirstAsync(p => p.Id == postIt.AuteurParticipantId);
        return new PostItResult(postIt.Id, postIt.ColonneId, postIt.Texte, auteur.NomAffiche, auteur.Id);
    }

    public async Task DeleteAsync(Guid boardId, Guid postItId, Guid callerParticipantId)
    {
        var postIt = await GetOwnedPostItAsync(boardId, postItId, callerParticipantId);
        db.PostIts.Remove(postIt);
        await db.SaveChangesAsync();
    }

    private async Task<PostIt> GetOwnedPostItAsync(Guid boardId, Guid postItId, Guid callerParticipantId)
    {
        var postIt = await db.PostIts.FirstOrDefaultAsync(p => p.Id == postItId && p.BoardId == boardId);
        if (postIt is null)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        if (postIt.AuteurParticipantId != callerParticipantId)
        {
            throw new DomainForbiddenException("Seul l'auteur d'un post-it peut le modifier ou le supprimer.");
        }

        return postIt;
    }

    private async Task<Board> GetBoardWithThemeAsync(Guid boardId)
    {
        var board = await db.Boards.Include(b => b.Theme!).ThenInclude(t => t.Colonnes).FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        return board;
    }

    private static void ValidateColonneAppartientAuBoard(Board board, Guid colonneId)
    {
        if (board.Theme!.Colonnes.All(c => c.Id != colonneId))
        {
            throw new DomainValidationException($"La colonne {colonneId} n'appartient pas au thème de ce board.");
        }
    }

    private static void ValidateTexte(string texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            throw new DomainValidationException("Le texte d'un post-it ne peut pas être vide.");
        }
    }
}
