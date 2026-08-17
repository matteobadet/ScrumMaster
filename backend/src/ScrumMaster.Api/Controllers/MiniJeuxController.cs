using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;

namespace ScrumMaster.Api.Controllers;

/// <summary>Catalogue de mini-jeux prédéfinis, pour la composition d'une étape "Mini-jeu" (US2, specs/006-systeme-extensions-etapes).</summary>
[ApiController]
[Route("api/mini-jeux")]
public class MiniJeuxController(ScrumMasterDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MiniJeuRefDto>>> GetMiniJeux()
    {
        var miniJeux = await db.MiniJeuxCatalogue.ToListAsync();

        return Ok(miniJeux.Select(m => new MiniJeuRefDto(m.Id, m.Nom, m.TypeInterne)).ToList());
    }
}
