using Api.Dtos;
using App.Features.Windows;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Tags("Building")]
[Route("api/[controller]")]
public class WindowsController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WindowResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListWindowsQuery(), ct);
        return Ok(mapper.Map<IEnumerable<WindowResponse>>(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WindowResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await mediator.Send(new GetWindowQuery(id), ct);
        if (w is null) return NotFound();
        return Ok(mapper.Map<WindowResponse>(w));
    }

    [HttpPost]
    public async Task<ActionResult<WindowResponse>> Create([FromBody] CreateWindowRequest request, CancellationToken ct)
    {
        var w = await mediator.Send(new CreateWindowCommand(
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
            request.HasStormWindows
        ), ct);
        return CreatedAtAction(nameof(Get), new { id = w.Id }, mapper.Map<WindowResponse>(w));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWindowRequest request, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdateWindowCommand(
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
                request.HasStormWindows
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
        await mediator.Send(new DeleteWindowCommand(id), ct);
        return NoContent();
    }
}