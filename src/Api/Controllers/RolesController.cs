using Api.Dtos;
using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController(IRoleService roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        var items = await roles.ListAsync(ct);
        return Ok(items.Select(r => new RoleDto(r.Id, r.Name, r.Description)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
    {
        var r = await roles.GetAsync(id, ct);
        if (r is null) return NotFound();
        return Ok(new RoleDto(r.Id, r.Name, r.Description));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var r = await roles.CreateAsync(request.Name, request.Description, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, new RoleDto(r.Id, r.Name, r.Description));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        await roles.UpdateAsync(id, request.Name, request.Description, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await roles.DeleteAsync(id, ct);
        return NoContent();
    }
}
