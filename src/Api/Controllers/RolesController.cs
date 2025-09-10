using Api.Dtos;
using Api.Services;
using App.Abstractions;
using App.Features.Roles;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Api.Controllers;

[ApiController]
[Tags("Roles")]
[Route("api/[controller]")]
[Authorize]
public class RolesController(IMediator mediator, IMapper mapper, IOutputCacheInvalidationService cacheInvalidation) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "RolesPolicy")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListRolesQuery(), ct);
        return Ok(mapper.Map<IEnumerable<RoleDto>>(items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "RolesPolicy")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
    {
        var r = await mediator.Send(new GetRoleQuery(id), ct);
        if (r is null)
            return NotFound();
        return Ok(mapper.Map<RoleDto>(r));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var r = await mediator.Send(new CreateRoleCommand(request.Name, request.Description), ct);

        // Invalidate collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("roles", ct);

        return CreatedAtAction(nameof(Get), new { id = r.Id }, mapper.Map<RoleDto>(r));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        await mediator.Send(new UpdateRoleCommand(id, request.Name, request.Description), ct);

        // Invalidate both entity and collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("roles", id, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteRoleCommand(id), ct);

        // Invalidate both entity and collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("roles", id, ct);

        return NoContent();
    }
}