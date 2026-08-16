using Microsoft.AspNetCore.Mvc;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Controllers;

[ApiController]
[Route("api")]
public class AzureDevOpsController(AzureDevOpsConfigService configService, AzureDevOpsBoardService boardService) : ControllerBase
{
    [HttpPut("equipes/{areaPath}/azure-devops-config")]
    public async Task<ActionResult<AzureDevOpsConfigResponse>> ConfigurerAsync(string areaPath, AzureDevOpsConfigRequest request)
    {
        var result = await configService.ConfigurerAsync(areaPath, request.Organisation, request.Projet, request.Pat);
        return Ok(new AzureDevOpsConfigResponse(result.AreaPath, result.Organisation, result.Projet));
    }

    [HttpGet("equipes/avec-azure-devops")]
    public async Task<ActionResult<IReadOnlyList<EquipeAzureDevOpsDto>>> ListerEquipesConfigureesAsync()
    {
        var equipes = await boardService.ListerEquipesConfigureesAsync();
        return Ok(equipes.Select(e => new EquipeAzureDevOpsDto(e.AreaPath)).ToList());
    }

    [HttpGet("equipes/{areaPath}/azure-devops/iterations")]
    public async Task<ActionResult<IReadOnlyList<IterationAzureDevOpsDto>>> ObtenirIterationsAsync(string areaPath)
    {
        var iterations = await boardService.ObtenirIterationsAsync(areaPath);
        return Ok(iterations.Select(i => new IterationAzureDevOpsDto(i.CheminIteration, i.EnCours)).ToList());
    }
}
