using Api.Dtos;
using App.Abstractions;
using App.Features.Roles;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Tags("People")]
[Route("api/[controller]")]
public class RolesController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListRolesQuery(), ct);
        return Ok(mapper.Map<IEnumerable<RoleDto>>(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
    {
        var r = await mediator.Send(new GetRoleQuery(id), ct);
        if (r is null)
            return NotFound();
        return Ok(mapper.Map<RoleDto>(r));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var r = await mediator.Send(new CreateRoleCommand(request.Name, request.Description), ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, mapper.Map<RoleDto>(r));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdateRoleCommand(id, request.Name, request.Description), ct);
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
            await mediator.Send(new DeleteRoleCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
