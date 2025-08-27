using Api.Dtos;
using App.Features.People;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Tags("People")]
[Route("api/[controller]")]
public class PeopleController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListPeopleQuery(), ct);
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
        var p = await mediator.Send(new GetPersonQuery(id), ct);
        if (p is null) return NotFound();
        return Ok(new PersonResponse(p.Id, p.FullName, p.Phone, p.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description))));
    }

    [HttpPost]
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    {
        try
        {
            var p = await mediator.Send(new CreatePersonCommand(request.FullName, request.Phone, request.RoleIds), ct);
            return CreatedAtAction(nameof(Get), new { id = p.Id }, new PersonResponse(p.Id, p.FullName, p.Phone, p.Roles.Select(r => new RoleResponse(r.Id, r.Name, r.Description))));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdatePersonCommand(id, request.FullName, request.Phone, request.RoleIds), ct);
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
            await mediator.Send(new DeletePersonCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
