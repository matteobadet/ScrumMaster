using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record DecompteOption(Guid OptionId, int Decompte);

public record RepondrePollPersonnaliseResult(IReadOnlyList<DecompteOption> DecompteParOption);

/// <summary>Réponse à une étape de type Poll personnalisé (US3, specs/006-systeme-extensions-etapes).</summary>
public class PollPersonnaliseService(ScrumMasterDbContext db)
{
    public async Task<RepondrePollPersonnaliseResult> RepondreAsync(Guid boardId, Guid etapeId, Guid participantId, Guid optionId)
    {
        var etape = await GetEtapeActiveAsync(boardId, etapeId);
        await EnsureParticipantAsync(boardId, participantId);

        if (etape.Options.All(o => o.Id != optionId))
        {
            throw new DomainValidationException($"L'option {optionId} n'appartient pas à cette étape.");
        }

        var existante = await db.ReponsesPollPersonnalise.FirstOrDefaultAsync(r => r.EtapeId == etapeId && r.ParticipantId == participantId);
        if (existante is null)
        {
            db.ReponsesPollPersonnalise.Add(
                new ReponsePollPersonnalise
                {
                    EtapeId = etapeId,
                    ParticipantId = participantId,
                    OptionId = optionId,
                    DateReponse = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            existante.OptionId = optionId;
            existante.DateReponse = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        var reponses = await db.ReponsesPollPersonnalise.Where(r => r.EtapeId == etapeId).ToListAsync();
        var decompte = etape.Options.Select(o => new DecompteOption(o.Id, reponses.Count(r => r.OptionId == o.Id))).ToList();

        return new RepondrePollPersonnaliseResult(decompte);
    }

    private async Task<Etape> GetEtapeActiveAsync(Guid boardId, Guid etapeId)
    {
        var etape = await db.Etapes.Include(e => e.Options).FirstOrDefaultAsync(e => e.Id == etapeId && e.BoardId == boardId);
        if (etape is null)
        {
            throw new DomainNotFoundException($"Étape {etapeId} introuvable sur ce board.");
        }

        if (etape.Type != TypeEtape.PollPersonnalise)
        {
            throw new DomainValidationException("Cette étape n'est pas un poll personnalisé.");
        }

        if (etape.Statut != StatutEtape.Active)
        {
            throw new DomainValidationException("Cette étape n'est plus active.");
        }

        return etape;
    }

    private async Task EnsureParticipantAsync(Guid boardId, Guid participantId)
    {
        var exists = await db.Participants.AnyAsync(p => p.Id == participantId && p.BoardId == boardId);
        if (!exists)
        {
            throw new DomainNotFoundException($"Participant {participantId} introuvable sur ce board.");
        }
    }
}
