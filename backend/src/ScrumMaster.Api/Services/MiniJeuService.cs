using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record RepondreMiniJeuResult(Guid ParticipantId, string NomAffiche, string Reponse);

/// <summary>
/// Réponse à une étape de type Mini-jeu (US2, specs/006-systeme-extensions-etapes). Le mini-jeu
/// concret ("Météo d'équipe" ou "ROTI") est résolu via <see cref="MiniJeuCatalogue.TypeInterne"/>
/// (specs/008-roti-mini-jeu, research.md#4).
/// </summary>
public class MiniJeuService(ScrumMasterDbContext db)
{
    public async Task<RepondreMiniJeuResult> RepondreAsync(Guid boardId, Guid etapeId, Guid participantId, string reponse)
    {
        var etape = await GetEtapeActiveAsync(boardId, etapeId);
        var participant = await GetParticipantAsync(boardId, participantId);

        return etape.MiniJeuCatalogue?.TypeInterne switch
        {
            "roti" => await RepondreRotiAsync(etapeId, participantId, participant, reponse),
            _ => await RepondreMeteoAsync(etapeId, participantId, participant, reponse),
        };
    }

    private async Task<RepondreMiniJeuResult> RepondreMeteoAsync(Guid etapeId, Guid participantId, Participant participant, string reponse)
    {
        if (!Enum.TryParse<HumeurMeteo>(reponse, ignoreCase: true, out var humeur))
        {
            throw new DomainValidationException($"Réponse \"{reponse}\" non reconnue pour ce mini-jeu.");
        }

        var existante = await db.ReponsesMeteoEquipe.FirstOrDefaultAsync(r => r.EtapeId == etapeId && r.ParticipantId == participantId);
        if (existante is null)
        {
            db.ReponsesMeteoEquipe.Add(
                new ReponseMeteoEquipe
                {
                    EtapeId = etapeId,
                    ParticipantId = participantId,
                    Humeur = humeur,
                    DateReponse = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            existante.Humeur = humeur;
            existante.DateReponse = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new RepondreMiniJeuResult(participantId, participant.NomAffiche, humeur.ToString());
    }

    private async Task<RepondreMiniJeuResult> RepondreRotiAsync(Guid etapeId, Guid participantId, Participant participant, string reponse)
    {
        if (!Enum.TryParse<NiveauRoti>(reponse, ignoreCase: true, out var niveau))
        {
            throw new DomainValidationException($"Réponse \"{reponse}\" non reconnue pour ce mini-jeu.");
        }

        var existante = await db.ReponsesRoti.FirstOrDefaultAsync(r => r.EtapeId == etapeId && r.ParticipantId == participantId);
        if (existante is null)
        {
            db.ReponsesRoti.Add(
                new ReponseRoti
                {
                    EtapeId = etapeId,
                    ParticipantId = participantId,
                    Niveau = niveau,
                    DateReponse = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            existante.Niveau = niveau;
            existante.DateReponse = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new RepondreMiniJeuResult(participantId, participant.NomAffiche, niveau.ToString());
    }

    private async Task<Etape> GetEtapeActiveAsync(Guid boardId, Guid etapeId)
    {
        var etape = await db.Etapes.Include(e => e.MiniJeuCatalogue).FirstOrDefaultAsync(e => e.Id == etapeId && e.BoardId == boardId);
        if (etape is null)
        {
            throw new DomainNotFoundException($"Étape {etapeId} introuvable sur ce board.");
        }

        if (etape.Type != TypeEtape.MiniJeu)
        {
            throw new DomainValidationException("Cette étape n'est pas un mini-jeu.");
        }

        if (etape.Statut != StatutEtape.Active)
        {
            throw new DomainValidationException("Cette étape n'est plus active.");
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
}
