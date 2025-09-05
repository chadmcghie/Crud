using Api.Dtos;
using App.Features.Walls;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Tags("Building")]
[Route("api/[controller]")]
public class WallsController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WallResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListWallsQuery(), ct);
        return Ok(mapper.Map<IEnumerable<WallResponse>>(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WallResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await mediator.Send(new GetWallQuery(id), ct);
        if (w is null)
            return NotFound();
        return Ok(mapper.Map<WallResponse>(w));
    }

    [HttpPost]
    public async Task<ActionResult<WallResponse>> Create([FromBody] CreateWallRequest request, CancellationToken ct)
    {
        var w = await mediator.Send(new CreateWallCommand(
            request.Name,
            request.Description,
            request.Length,
            request.Height,
            request.Thickness,
            request.AssemblyType,
            request.AssemblyDetails,
            request.RValue,
            request.UValue,
            request.MaterialLayers,
            request.Orientation,
            request.Location
        ), ct);
        return CreatedAtAction(nameof(Get), new { id = w.Id }, mapper.Map<WallResponse>(w));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWallRequest request, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdateWallCommand(
                id,
                request.Name,
                request.Description,
                request.Length,
                request.Height,
                request.Thickness,
                request.AssemblyType,
                request.AssemblyDetails,
                request.RValue,
                request.UValue,
                request.MaterialLayers,
                request.Orientation,
                request.Location
            ), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteWallCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
