using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record PostItResult(Guid Id, Guid ColonneId, string Texte, string Auteur, Guid AuteurParticipantId);

/// <summary>
/// Portée révisée par étape (specs/006-systeme-extensions-etapes) : un post-it appartient à
/// l'étape "Colonnes et post-its" qui le porte, pas directement au board.
/// </summary>
public class PostItService(ScrumMasterDbContext db)
{
    public async Task<PostItResult> AddAsync(Guid boardId, Guid colonneId, string texte, Guid auteurParticipantId)
    {
        ValidateTexte(texte);

        var etape = await GetEtapeActiveColonnesEtPostItsAsync(boardId);
        ValidateColonneAppartientALEtape(etape, colonneId);

        var auteur = await GetParticipantAsync(boardId, auteurParticipantId);

        var now = DateTimeOffset.UtcNow;
        var postIt = new PostIt
        {
            Id = Guid.NewGuid(),
            EtapeId = etape.Id,
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

        var postIt = await GetOwnedPostItActifAsync(boardId, postItId, callerParticipantId);
        postIt.Texte = texte.Trim();
        postIt.DateModification = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var auteur = await db.Participants.FirstAsync(p => p.Id == postIt.AuteurParticipantId);
        return new PostItResult(postIt.Id, postIt.ColonneId, postIt.Texte, auteur.NomAffiche, auteur.Id);
    }

    public async Task<PostItResult> MoveAsync(Guid boardId, Guid postItId, Guid colonneId)
    {
        var postIt = await db
            .PostIts.Include(p => p.Etape!)
            .ThenInclude(e => e.Theme!)
            .ThenInclude(t => t.Colonnes)
            .FirstOrDefaultAsync(p => p.Id == postItId);
        if (postIt is null || postIt.Etape!.BoardId != boardId)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        EnsureEtapeActive(postIt.Etape);
        ValidateColonneAppartientALEtape(postIt.Etape, colonneId);

        postIt.ColonneId = colonneId;
        postIt.DateModification = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var auteur = await db.Participants.FirstAsync(p => p.Id == postIt.AuteurParticipantId);
        return new PostItResult(postIt.Id, postIt.ColonneId, postIt.Texte, auteur.NomAffiche, auteur.Id);
    }

    public async Task DeleteAsync(Guid boardId, Guid postItId, Guid callerParticipantId)
    {
        var postIt = await GetOwnedPostItActifAsync(boardId, postItId, callerParticipantId);
        db.PostIts.Remove(postIt);
        await db.SaveChangesAsync();
    }

    private async Task<PostIt> GetOwnedPostItActifAsync(Guid boardId, Guid postItId, Guid callerParticipantId)
    {
        var postIt = await db.PostIts.Include(p => p.Etape!).FirstOrDefaultAsync(p => p.Id == postItId);
        if (postIt is null || postIt.Etape!.BoardId != boardId)
        {
            throw new DomainNotFoundException($"Post-it {postItId} introuvable sur ce board.");
        }

        EnsureEtapeActive(postIt.Etape);

        if (postIt.AuteurParticipantId != callerParticipantId)
        {
            throw new DomainForbiddenException("Seul l'auteur d'un post-it peut le modifier ou le supprimer.");
        }

        return postIt;
    }

    private static void EnsureEtapeActive(Etape etape)
    {
        if (etape.Statut != StatutEtape.Active)
        {
            throw new DomainValidationException("Cette étape n'est plus active.");
        }
    }

    private async Task<Etape> GetEtapeActiveColonnesEtPostItsAsync(Guid boardId)
    {
        var board = await db
            .Boards.Include(b => b.Etapes)
            .ThenInclude(e => e.Theme!)
            .ThenInclude(t => t.Colonnes)
            .FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        BoardClosureGuard.EnsureActif(board);

        var etape = board.Etapes.FirstOrDefault(e => e.Statut == StatutEtape.Active);
        if (etape is null || etape.Type != TypeEtape.ColonnesEtPostIts)
        {
            throw new DomainValidationException("L'étape active n'accepte pas les post-its.");
        }

        return etape;
    }

    private async Task<Participant> GetParticipantAsync(Guid boardId, Guid participantId)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.BoardId == boardId);
        if (participant is null)
        {
            throw new DomainNotFoundException($"Participant {participantId} introuvable sur ce board.");
        }

        return participant;
    }

    private static void ValidateColonneAppartientALEtape(Etape etape, Guid colonneId)
    {
        if (etape.Theme!.Colonnes.All(c => c.Id != colonneId))
        {
            throw new DomainValidationException($"La colonne {colonneId} n'appartient pas au thème de cette étape.");
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
