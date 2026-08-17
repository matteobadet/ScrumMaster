using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public class BoardService(ScrumMasterDbContext db, EtapeService etapeService)
{
    public async Task<CreateBoardResponse> CreateBoardAsync(CreateBoardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AreaPath))
        {
            throw new DomainValidationException("L'Area Path est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(request.Iteration))
        {
            throw new DomainValidationException("L'Iteration/Sprint est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(request.NomAffiche))
        {
            throw new DomainValidationException("Le nom affiché est obligatoire.");
        }

        var etapes = await etapeService.ConstruireSequenceAsync(request.Etapes, request.ThemeId, request.ThemePersonnalise);

        var equipe = await db.Equipes.FindAsync(request.AreaPath);
        if (equipe is null)
        {
            equipe = new Equipe { AreaPath = request.AreaPath };
            db.Equipes.Add(equipe);
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            AreaPath = request.AreaPath,
            Iteration = request.Iteration,
            Statut = BoardStatut.Actif,
            DateCreation = DateTimeOffset.UtcNow,
            MaxVotesParParticipant = request.MaxVotesParParticipant is > 0 ? request.MaxVotesParParticipant.Value : 3,
        };
        foreach (var etape in etapes)
        {
            etape.BoardId = board.Id;
        }
        board.Etapes = etapes;
        db.Boards.Add(board);

        var facilitateur = new Participant
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            NomAffiche = request.NomAffiche,
            Role = ParticipantRole.Facilitateur,
        };
        db.Participants.Add(facilitateur);

        await db.SaveChangesAsync();

        return new CreateBoardResponse(board.Id, facilitateur.Id, facilitateur.Role.ToString(), $"/board/{board.Id}");
    }

    public async Task<BoardStateDto> GetBoardStateAsync(Guid boardId, Guid? asParticipantId = null)
    {
        var board = await db
            .Boards.Include(b => b.Etapes)
            .ThenInclude(e => e.Theme!)
            .ThenInclude(t => t.Colonnes)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.PostIts)
            .ThenInclude(p => p.Votes)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.PostIts)
            .ThenInclude(p => p.Auteur)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.MiniJeuCatalogue)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.ReponsesMeteo)
            .ThenInclude(r => r.Participant)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.Options)
            .Include(b => b.Etapes)
            .ThenInclude(e => e.Reponses)
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var etapeDtos = board
            .Etapes.OrderBy(e => e.Ordre)
            .Select(e => BuildEtapeDto(e, board.MaxVotesParParticipant, asParticipantId))
            .ToList();

        return new BoardStateDto(
            board.Id,
            board.AreaPath,
            board.Iteration,
            board.Statut.ToString(),
            board.MaxVotesParParticipant,
            etapeDtos,
            board.Participants.Select(p => new ParticipantDto(p.Id, p.NomAffiche, p.Role.ToString())).ToList()
        );
    }

    public async Task<ChangeThemeResult> ChangeThemeAsync(
        Guid boardId,
        Guid callerParticipantId,
        Guid? themeId,
        ThemePersonnaliseDto? themePersonnalise
    )
    {
        var board = await db.Boards.Include(b => b.Etapes).FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var caller = await db.Participants.FirstOrDefaultAsync(p => p.Id == callerParticipantId && p.BoardId == boardId);
        if (caller is null)
        {
            throw new DomainNotFoundException($"Participant {callerParticipantId} introuvable sur ce board.");
        }

        if (caller.Role != ParticipantRole.Facilitateur)
        {
            throw new DomainForbiddenException("Seul le facilitateur peut changer le thème du board.");
        }

        BoardClosureGuard.EnsureActif(board);

        var etapeActive = etapeService.ObtenirEtapeActive(board);
        if (etapeActive.Type != TypeEtape.ColonnesEtPostIts)
        {
            throw new DomainValidationException("L'étape active n'est pas une étape \"Colonnes et post-its\".");
        }

        var theme = await etapeService.ResolveThemeAsync(themeId, themePersonnalise);
        db.Themes.Add(theme);
        etapeActive.ThemeId = theme.Id;

        // ChangeTheme crée toujours de nouvelles colonnes (même pour un thème prédéfini copié) :
        // les post-its existants de cette étape doivent être réaffectés pour ne pas devenir
        // orphelins et disparaître silencieusement de l'affichage.
        var premiereColonne = theme.Colonnes.OrderBy(c => c.Ordre).First();
        var postItsExistants = await db.PostIts.Where(p => p.EtapeId == etapeActive.Id).ToListAsync();
        foreach (var postIt in postItsExistants)
        {
            postIt.ColonneId = premiereColonne.Id;
        }

        await db.SaveChangesAsync();

        return new ChangeThemeResult(
            theme.Id,
            theme.Nom,
            theme.Icone,
            theme.Contexte,
            theme.Colonnes.OrderBy(c => c.Ordre).Select(c => new ColonneDto(c.Id, c.Intitule, c.Ordre)).ToList()
        );
    }

    private static EtapeDto BuildEtapeDto(Etape etape, int maxVotesParParticipant, Guid? asParticipantId)
    {
        switch (etape.Type)
        {
            case TypeEtape.ColonnesEtPostIts:
                var colonnes = etape.Theme!.Colonnes.OrderBy(c => c.Ordre).ToList();
                int? mesVotesRestants = null;
                if (asParticipantId is { } pidVote)
                {
                    var votesUtilises = etape.PostIts.SelectMany(p => p.Votes).Count(v => v.ParticipantId == pidVote);
                    mesVotesRestants = Math.Max(0, maxVotesParParticipant - votesUtilises);
                }

                return new EtapeDto(
                    etape.Id,
                    etape.Type.ToString(),
                    etape.Ordre,
                    etape.Statut.ToString(),
                    new ThemeRefDto(etape.Theme.Id, etape.Theme.Nom, etape.Theme.Icone, etape.Theme.Contexte),
                    colonnes.Select(c => new ColonneDto(c.Id, c.Intitule, c.Ordre)).ToList(),
                    etape
                        .PostIts.OrderBy(p => p.DateCreation)
                        .Select(p => new PostItDto(
                            p.Id,
                            p.ColonneId,
                            p.Texte,
                            p.Auteur?.NomAffiche ?? string.Empty,
                            p.AuteurParticipantId,
                            p.Votes.Count,
                            asParticipantId is { } pid && p.Votes.Any(v => v.ParticipantId == pid),
                            p.WorkItemExporteId
                        ))
                        .ToList(),
                    mesVotesRestants,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );

            case TypeEtape.MiniJeu:
                var monHumeur = asParticipantId is { } pidMeteo
                    ? etape.ReponsesMeteo.FirstOrDefault(r => r.ParticipantId == pidMeteo)?.Humeur.ToString()
                    : null;

                return new EtapeDto(
                    etape.Id,
                    etape.Type.ToString(),
                    etape.Ordre,
                    etape.Statut.ToString(),
                    null,
                    null,
                    null,
                    null,
                    etape.MiniJeuCatalogue is { } catalogue ? new MiniJeuRefDto(catalogue.Id, catalogue.Nom, catalogue.TypeInterne) : null,
                    etape
                        .ReponsesMeteo.Select(r => new ReponseMeteoDto(r.ParticipantId, r.Participant?.NomAffiche ?? string.Empty, r.Humeur.ToString()))
                        .ToList(),
                    monHumeur,
                    null,
                    null,
                    null
                );

            case TypeEtape.PollPersonnalise:
                var maReponseOptionId = asParticipantId is { } pidPoll
                    ? etape.Reponses.FirstOrDefault(r => r.ParticipantId == pidPoll)?.OptionId
                    : null;

                return new EtapeDto(
                    etape.Id,
                    etape.Type.ToString(),
                    etape.Ordre,
                    etape.Statut.ToString(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    etape.Question,
                    etape
                        .Options.OrderBy(o => o.Ordre)
                        .Select(o => new OptionPollDto(o.Id, o.Texte, etape.Reponses.Count(r => r.OptionId == o.Id)))
                        .ToList(),
                    maReponseOptionId
                );

            default:
                throw new InvalidOperationException($"Type d'étape non géré : {etape.Type}.");
        }
    }
}
