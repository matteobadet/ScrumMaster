using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrumMaster.Api.Data;
using ScrumMaster.Api.Dtos;

namespace ScrumMaster.Api.Controllers;

[ApiController]
[Route("api/themes")]
public class ThemesController(ScrumMasterDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ThemeSummaryDto>>> GetThemes()
    {
        var themes = await db.Themes.Include(t => t.Colonnes).Where(t => t.EstPredefini).ToListAsync();

        var result = themes
            .Select(t => new ThemeSummaryDto(t.Id, t.Nom, t.Colonnes.OrderBy(c => c.Ordre).Select(c => c.Intitule).ToList()))
            .ToList();

        return Ok(result);
    }
}
