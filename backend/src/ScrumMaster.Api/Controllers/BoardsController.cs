using Microsoft.AspNetCore.Mvc;
using ScrumMaster.Api.Dtos;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Controllers;

[ApiController]
[Route("api/boards")]
public class BoardsController(BoardService boardService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateBoardResponse>> CreateBoard(CreateBoardRequest request)
    {
        var response = await boardService.CreateBoardAsync(request);
        return CreatedAtAction(nameof(GetBoard), new { boardId = response.BoardId }, response);
    }

    [HttpGet("{boardId:guid}")]
    public async Task<ActionResult<BoardStateDto>> GetBoard(Guid boardId)
    {
        var state = await boardService.GetBoardStateAsync(boardId);
        return Ok(state);
    }
}
