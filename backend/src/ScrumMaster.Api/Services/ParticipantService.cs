using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record JoinBoardResult(Guid ParticipantId, string Role);

public class ParticipantService(ScrumMasterDbContext db)
{
    public async Task<JoinBoardResult> JoinAsync(Guid boardId, string nomAffiche)
    {
        if (string.IsNullOrWhiteSpace(nomAffiche))
        {
            throw new DomainValidationException("Le nom affiché est obligatoire.");
        }

        var boardExists = await db.Boards.AnyAsync(b => b.Id == boardId);
        if (!boardExists)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var participant = new Participant
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            NomAffiche = nomAffiche.Trim(),
            Role = ParticipantRole.Participant,
        };
        db.Participants.Add(participant);
        await db.SaveChangesAsync();

        return new JoinBoardResult(participant.Id, participant.Role.ToString());
    }
}
