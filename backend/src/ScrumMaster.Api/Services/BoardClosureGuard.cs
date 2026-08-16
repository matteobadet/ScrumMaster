using ScrumMaster.Api.Models;

namespace ScrumMaster.Api.Services;

/// <summary>FR-016 : aucune mutation de contenu n'est autorisée une fois le board clôturé.</summary>
public static class BoardClosureGuard
{
    public static void EnsureActif(Board board)
    {
        if (board.Statut == BoardStatut.Cloture)
        {
            throw new DomainForbiddenException("Ce board est clôturé et passé en lecture seule.");
        }
    }
}
