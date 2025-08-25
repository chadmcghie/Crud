using Api.Dtos;
using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WindowsController(IWindowService windows) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WindowResponse>>> List(CancellationToken ct)
    {
        var items = await windows.ListAsync(ct);
        return Ok(items.Select(w => new WindowResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Width,
            w.Height,
            w.Area,
            w.FrameType,
            w.FrameDetails,
            w.GlazingType,
            w.GlazingDetails,
            w.UValue,
            w.SolarHeatGainCoefficient,
            w.VisibleTransmittance,
            w.AirLeakage,
            w.EnergyStarRating,
            w.NFRCRating,
            w.Orientation,
            w.Location,
            w.InstallationType,
            w.OperationType,
            w.HasScreens,
            w.HasStormWindows,
            w.CreatedAt,
            w.UpdatedAt
        )));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WindowResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await windows.GetAsync(id, ct);
        if (w is null) return NotFound();
        return Ok(new WindowResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Width,
            w.Height,
            w.Area,
            w.FrameType,
            w.FrameDetails,
            w.GlazingType,
            w.GlazingDetails,
            w.UValue,
            w.SolarHeatGainCoefficient,
            w.VisibleTransmittance,
            w.AirLeakage,
            w.EnergyStarRating,
            w.NFRCRating,
            w.Orientation,
            w.Location,
            w.InstallationType,
            w.OperationType,
            w.HasScreens,
            w.HasStormWindows,
            w.CreatedAt,
            w.UpdatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<WindowResponse>> Create([FromBody] CreateWindowRequest request, CancellationToken ct)
    {
        var w = await windows.CreateAsync(
            request.Name,
            request.Description,
            request.Width,
            request.Height,
            request.Area,
            request.FrameType,
            request.FrameDetails,
            request.GlazingType,
            request.GlazingDetails,
            request.UValue,
            request.SolarHeatGainCoefficient,
            request.VisibleTransmittance,
            request.AirLeakage,
            request.EnergyStarRating,
            request.NFRCRating,
            request.Orientation,
            request.Location,
            request.InstallationType,
            request.OperationType,
            request.HasScreens,
            request.HasStormWindows,
            ct
        );
        return CreatedAtAction(nameof(Get), new { id = w.Id }, new WindowResponse(
            w.Id,
            w.Name,
            w.Description,
            w.Width,
            w.Height,
            w.Area,
            w.FrameType,
            w.FrameDetails,
            w.GlazingType,
            w.GlazingDetails,
            w.UValue,
            w.SolarHeatGainCoefficient,
            w.VisibleTransmittance,
            w.AirLeakage,
            w.EnergyStarRating,
            w.NFRCRating,
            w.Orientation,
            w.Location,
            w.InstallationType,
            w.OperationType,
            w.HasScreens,
            w.HasStormWindows,
            w.CreatedAt,
            w.UpdatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWindowRequest request, CancellationToken ct)
    {
        await windows.UpdateAsync(
            id,
            request.Name,
            request.Description,
            request.Width,
            request.Height,
            request.Area,
            request.FrameType,
            request.FrameDetails,
            request.GlazingType,
            request.GlazingDetails,
            request.UValue,
            request.SolarHeatGainCoefficient,
            request.VisibleTransmittance,
            request.AirLeakage,
            request.EnergyStarRating,
            request.NFRCRating,
            request.Orientation,
            request.Location,
            request.InstallationType,
            request.OperationType,
            request.HasScreens,
            request.HasStormWindows,
            ct
        );
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await windows.DeleteAsync(id, ct);
        return NoContent();
    }
}