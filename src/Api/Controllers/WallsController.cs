using Api.Dtos;
using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Tags("Building")]
[Route("api/[controller]")]
public class WallsController(IWallService walls) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WallResponse>>> List(CancellationToken ct)
    {
        var items = await walls.ListAsync(ct);
        return Ok(items.Select(w => new WallResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Length,
            w.Height,
            w.Thickness,
            w.AssemblyType,
            w.AssemblyDetails,
            w.RValue,
            w.UValue,
            w.MaterialLayers,
            w.Orientation,
            w.Location,
            w.CreatedAt,
            w.UpdatedAt
        )));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WallResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await walls.GetAsync(id, ct);
        if (w is null) return NotFound();
        return Ok(new WallResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Length,
            w.Height,
            w.Thickness,
            w.AssemblyType,
            w.AssemblyDetails,
            w.RValue,
            w.UValue,
            w.MaterialLayers,
            w.Orientation,
            w.Location,
            w.CreatedAt,
            w.UpdatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<WallResponse>> Create([FromBody] CreateWallRequest request, CancellationToken ct)
    {
        var w = await walls.CreateAsync(
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
            request.Location,
            ct
        );
        return CreatedAtAction(nameof(Get), new { id = w.Id }, new WallResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Length,
            w.Height,
            w.Thickness,
            w.AssemblyType,
            w.AssemblyDetails,
            w.RValue,
            w.UValue,
            w.MaterialLayers,
            w.Orientation,
            w.Location,
            w.CreatedAt,
            w.UpdatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWallRequest request, CancellationToken ct)
    {
        try
        {
            await walls.UpdateAsync(
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
                request.Location,
                ct
            );
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
            await walls.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
