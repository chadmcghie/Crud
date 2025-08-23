using Api.Dtos;
using App.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController(IPersonService people) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> List(CancellationToken ct)
    {
        var items = await people.ListAsync(ct);
        return Ok(items.Select(p => new PersonResponse(
            p.Id,
            p.FullName,
            p.Phone,
            p.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description))
        )));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonResponse>> Get(Guid id, CancellationToken ct)
    {
        var p = await people.GetAsync(id, ct);
        if (p is null) return NotFound();
        return Ok(new PersonResponse(p.Id, p.FullName, p.Phone, p.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description))));
    }

    [HttpPost]
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    {
        var p = await people.CreateAsync(request.FullName, request.Phone, request.RoleIds, ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new PersonResponse(p.Id, p.FullName, p.Phone, p.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description))));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        await people.UpdateAsync(id, request.FullName, request.Phone, request.RoleIds, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await people.DeleteAsync(id, ct);
        return NoContent();
    }
}
