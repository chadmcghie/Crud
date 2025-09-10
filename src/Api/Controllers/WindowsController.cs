using Api.Dtos;
using Api.Services;
using App.Features.Windows;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Api.Controllers;

[ApiController]
[Tags("Building")]
[Route("api/[controller]")]
[Authorize]
public class WindowsController(IMediator mediator, IMapper mapper, IOutputCacheInvalidationService cacheInvalidation) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "WindowsPolicy")]
    public async Task<ActionResult<IEnumerable<WindowResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListWindowsQuery(), ct);
        return Ok(mapper.Map<IEnumerable<WindowResponse>>(items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "WindowsPolicy")]
    public async Task<ActionResult<WindowResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await mediator.Send(new GetWindowQuery(id), ct);
        if (w is null)
            return NotFound();
        return Ok(mapper.Map<WindowResponse>(w));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
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

        // Invalidate collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("windows", ct);

        return CreatedAtAction(nameof(Get), new { id = w.Id }, mapper.Map<WindowResponse>(w));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
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

            // Invalidate both entity and collection cache
            await cacheInvalidation.InvalidateEntityCacheAsync("windows", id, ct);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteWindowCommand(id), ct);

        // Invalidate both entity and collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("windows", id, ct);

        return NoContent();
    }
}