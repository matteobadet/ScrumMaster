using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public record PollDeclencheResult(Guid PollId, TypeReunion TypeReunion);

public record VoteUtiliteResult(Guid PollId, TypeReunion TypeReunion, int NombreUtile, int NombrePasNecessaire);

/// <summary>
/// Association channel/équipe, déclenchement, vote et clôture des polls d'utilité — voir
/// specs/002-poll-utilite-reunion. Implémenté progressivement par les User Stories 1 à 3.
/// </summary>
public class PollService(ScrumMasterDbContext db)
{
    /// <summary>
    /// Associe le channel Teams courant à l'équipe (FR-001, FR-002). Aucun contrôle de rôle
    /// n'est appliqué ici : contrairement au facilitateur de board (specs/001-retro-board-base,
    /// identité par session de board), il n'existe pas de mapping durable entre une identité
    /// Teams et un rôle d'équipe dans cette feature — seule l'appartenance au channel Teams
    /// (contrôlée par Teams lui-même) restreint qui peut exécuter cette commande.
    /// </summary>
    public async Task AssocierChannelAsync(string areaPath, string teamsChannelId)
    {
        if (string.IsNullOrWhiteSpace(areaPath))
        {
            throw new DomainValidationException("L'Area Path est obligatoire.");
        }

        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.AreaPath == areaPath);
        if (equipe is null)
        {
            throw new DomainNotFoundException(
                $"Équipe \"{areaPath}\" introuvable. Elle doit déjà exister (créée via un board de rétrospective)."
            );
        }

        equipe.TeamsChannelId = teamsChannelId;
        await db.SaveChangesAsync();
    }

    /// <summary>Déclenche un poll d'utilité pour le channel courant (FR-003).</summary>
    public async Task<PollDeclencheResult> DeclencherPollAsync(string teamsChannelId, TypeReunion typeReunion)
    {
        var equipe = await db.Equipes.FirstOrDefaultAsync(e => e.TeamsChannelId == teamsChannelId);
        if (equipe is null)
        {
            throw new DomainValidationException(
                "Ce channel n'est associé à aucune équipe. Utilisez d'abord \"associer <area-path>\"."
            );
        }

        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var pollDejaOuvert = await db.PollsUtilite.AnyAsync(p =>
            p.AreaPath == equipe.AreaPath && p.TypeReunion == typeReunion && p.Date == aujourdhui && p.Statut == StatutPoll.Ouvert
        );
        if (pollDejaOuvert)
        {
            throw new DomainValidationException("Un poll est déjà ouvert pour cette réunion aujourd'hui.");
        }

        var poll = new PollUtilite
        {
            Id = Guid.NewGuid(),
            AreaPath = equipe.AreaPath,
            TypeReunion = typeReunion,
            Date = aujourdhui,
            Statut = StatutPoll.Ouvert,
            DateCreation = DateTimeOffset.UtcNow,
        };
        db.PollsUtilite.Add(poll);
        await db.SaveChangesAsync();

        return new PollDeclencheResult(poll.Id, poll.TypeReunion);
    }

    /// <summary>Enregistre ou remplace le vote d'un membre sur un poll ouvert (FR-006, FR-007, FR-008).</summary>
    public async Task<VoteUtiliteResult> VoterAsync(Guid pollId, string teamsUserId, string nomAffiche, ReponseVote reponse)
    {
        var poll = await db.PollsUtilite.FirstOrDefaultAsync(p => p.Id == pollId);
        if (poll is null)
        {
            throw new DomainNotFoundException($"Poll {pollId} introuvable.");
        }

        if (poll.Statut == StatutPoll.Cloture)
        {
            throw new DomainForbiddenException("Ce poll est clos, votre vote n'a pas été pris en compte.");
        }

        var voteExistant = await db.VotesUtilite.FirstOrDefaultAsync(v => v.PollId == pollId && v.TeamsUserId == teamsUserId);
        if (voteExistant is null)
        {
            db.VotesUtilite.Add(
                new VoteUtilite
                {
                    PollId = pollId,
                    TeamsUserId = teamsUserId,
                    NomAffiche = nomAffiche,
                    Reponse = reponse,
                    DateVote = DateTimeOffset.UtcNow,
                }
            );
        }
        else
        {
            voteExistant.Reponse = reponse;
            voteExistant.NomAffiche = nomAffiche;
            voteExistant.DateVote = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        var votes = await db.VotesUtilite.Where(v => v.PollId == pollId).ToListAsync();
        return new VoteUtiliteResult(
            pollId,
            poll.TypeReunion,
            votes.Count(v => v.Reponse == ReponseVote.Utile),
            votes.Count(v => v.Reponse == ReponseVote.PasNecessaire)
        );
    }
}
