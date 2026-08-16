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
            new ThemeRefDto(board.Theme.Id, board.Theme.Nom),
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

    private async Task<Theme> ResolveThemeAsync(Guid? themeId, ThemePersonnaliseDto? themePersonnalise)
    {
        if (themeId is { } id)
        {
            var source = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.Id == id);
            if (source is null)
            {
                throw new DomainValidationException($"Thème {id} introuvable.");
            }

            return CopyTheme(source.Nom, source.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
        }

        if (themePersonnalise is not null)
        {
            var colonnes = themePersonnalise.Colonnes.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            if (colonnes.Count == 0)
            {
                throw new DomainValidationException("Un thème doit comporter au moins une colonne.");
            }

            return CopyTheme(themePersonnalise.Nom, colonnes);
        }

        var defaut = await db.Themes.Include(t => t.Colonnes).FirstOrDefaultAsync(t => t.EstParDefaut);
        if (defaut is null)
        {
            throw new DomainValidationException("Aucun thème par défaut n'est configuré.");
        }

        return CopyTheme(defaut.Nom, defaut.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule));
    }

    private static Theme CopyTheme(string nom, IEnumerable<string> colonnes)
    {
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Nom = nom,
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
