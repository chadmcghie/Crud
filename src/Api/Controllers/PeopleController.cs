using Api.Dtos;
using App.Features.People;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Api.Controllers;

[ApiController]
[Tags("People")]
[Route("api/[controller]")]
public class PeopleController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [OutputCache(PolicyName = "PeoplePolicy")]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListPeopleQuery(), ct);
        return Ok(mapper.Map<IEnumerable<PersonResponse>>(items));
    }

    [HttpGet("{id:guid}")]
    [OutputCache(PolicyName = "PeoplePolicy")]
    public async Task<ActionResult<PersonResponse>> Get(Guid id, CancellationToken ct)
    {
        var p = await mediator.Send(new GetPersonQuery(id), ct);
        if (p is null)
            return NotFound();
        return Ok(mapper.Map<PersonResponse>(p));
    }

    [HttpPost]
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    {
        var p = await mediator.Send(new CreatePersonCommand(request.FullName, request.Phone, request.RoleIds), ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, mapper.Map<PersonResponse>(p));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        await mediator.Send(new UpdatePersonCommand(id, request.FullName, request.Phone, request.RoleIds), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeletePersonCommand(id), ct);
        return NoContent();
    }
}
