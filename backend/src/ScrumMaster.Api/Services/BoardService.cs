using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

public class BoardService(ScrumMasterDbContext db)
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

        var theme = await ResolveThemeAsync(request.ThemeId, request.ThemePersonnalise);

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
            ThemeId = theme.Id,
            Statut = BoardStatut.Actif,
            DateCreation = DateTimeOffset.UtcNow,
            MaxVotesParParticipant = request.MaxVotesParParticipant is > 0 ? request.MaxVotesParParticipant.Value : 3,
        };
        db.Themes.Add(theme);
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
            .Boards.Include(b => b.Theme!)
            .ThenInclude(t => t.Colonnes)
            .Include(b => b.Participants)
            .Include(b => b.PostIts)
            .ThenInclude(p => p.Votes)
            .Include(b => b.PostIts)
            .ThenInclude(p => p.Auteur)
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board is null)
        {
            throw new DomainNotFoundException($"Board {boardId} introuvable.");
        }

        var colonnes = board.Theme!.Colonnes.OrderBy(c => c.Ordre).ToList();

        int? mesVotesRestants = null;
        if (asParticipantId is { } participantId)
        {
            var votesUtilises = board.PostIts.SelectMany(p => p.Votes).Count(v => v.ParticipantId == participantId);
            mesVotesRestants = Math.Max(0, board.MaxVotesParParticipant - votesUtilises);
        }

        return new BoardStateDto(
            board.Id,
            board.AreaPath,
            board.Iteration,
            board.Statut.ToString(),
            board.MaxVotesParParticipant,
            mesVotesRestants,
            new ThemeRefDto(board.Theme.Id, board.Theme.Nom, board.Theme.Icone, board.Theme.Contexte),
            colonnes.Select(c => new ColonneDto(c.Id, c.Intitule, c.Ordre)).ToList(),
            board
                .PostIts.OrderBy(p => p.DateCreation)
                .Select(p => new PostItDto(
                    p.Id,
                    p.ColonneId,
                    p.Texte,
                    p.Auteur?.NomAffiche ?? string.Empty,
                    p.AuteurParticipantId,
                    p.Votes.Count,
                    asParticipantId is { } pid && p.Votes.Any(v => v.ParticipantId == pid)
                ))
                .ToList(),
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
        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
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

        var theme = await ResolveThemeAsync(themeId, themePersonnalise);
        db.Themes.Add(theme);
        board.ThemeId = theme.Id;

        // ChangeTheme crée toujours de nouvelles colonnes (même pour un thème prédéfini copié) :
        // les post-its existants doivent être réaffectés pour ne pas devenir orphelins et
        // disparaître silencieusement de l'affichage.
        var premiereColonne = theme.Colonnes.OrderBy(c => c.Ordre).First();
        var postItsExistants = await db.PostIts.Where(p => p.BoardId == boardId).ToListAsync();
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

    public async Task CloseBoardAsync(Guid boardId, Guid callerParticipantId)
    {
        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
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
            throw new DomainForbiddenException("Seul le facilitateur peut clôturer le board.");
        }

        if (board.Statut == BoardStatut.Cloture)
        {
            throw new DomainValidationException("Ce board est déjà clôturé.");
        }

        board.Statut = BoardStatut.Cloture;
        await db.SaveChangesAsync();
    }

    private async Task<Theme> ResolveThemeAsync(Guid? themeId, ThemePersonnaliseDto? themePersonnalise)
    {
        if (themeId is { } id)
        {
            var source = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.Id == id);
            if (source is null)
            {
                throw new DomainValidationException($"Thème {id} introuvable.");
            }

            return CopyTheme(source.Nom, source.Icone, source.Contexte, source.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
        }

        if (themePersonnalise is not null)
        {
            var colonnes = themePersonnalise.Colonnes.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            if (colonnes.Count == 0)
            {
                throw new DomainValidationException("Un thème doit comporter au moins une colonne.");
            }

            if (themePersonnalise.Icone?.Length > 50)
            {
                throw new DomainValidationException("L'icône du thème ne doit pas dépasser 50 caractères.");
            }

            if (themePersonnalise.Contexte?.Length > 500)
            {
                throw new DomainValidationException("Le contexte du thème ne doit pas dépasser 500 caractères.");
            }

            return CopyTheme(themePersonnalise.Nom, themePersonnalise.Icone, themePersonnalise.Contexte, colonnes);
        }

        var defaut = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.EstParDefaut);
        if (defaut is null)
        {
            throw new DomainValidationException("Aucun thème par défaut n'est configuré.");
        }

        return CopyTheme(defaut.Nom, defaut.Icone, defaut.Contexte, defaut.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
    }

    private static Theme CopyTheme(string nom, string? icone, string? contexte, IEnumerable<string> colonnes)
    {
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Icone = icone,
            Contexte = contexte,
            EstPredefini = false,
            EstParDefaut = false,
        };

        theme.Colonnes = colonnes
            .Select(
                (intitule, index) =>
                    new Colonne
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = theme.Id,
                        Intitule = intitule,
                        Ordre = index,
                    }
            )
            .ToList();

        return theme;
    }
}
