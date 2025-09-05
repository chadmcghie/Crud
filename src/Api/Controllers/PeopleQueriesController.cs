using Api.Dtos;
using App.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Example controller demonstrating the use of Ardalis.Specification pattern with generic repository
/// This controller showcases query capabilities using specifications for complex queries
/// </summary>
[ApiController]
[Tags("People Queries (Specification Pattern Example)")]
[Route("api/people/queries")]
public class PeopleQueriesController : ControllerBase
{
    private readonly IPersonQueryService _personQueryService;
    private readonly IMapper _mapper;

    public PeopleQueriesController(IPersonQueryService personQueryService, IMapper mapper)
    {
        _personQueryService = personQueryService;
        _mapper = mapper;
    }

    /// <summary>
    /// Find people by partial name match (case-insensitive)
    /// Example: /api/people/queries/search?name=john
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> SearchByName(
        [FromQuery] string name,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name parameter is required");

        var people = await _personQueryService.FindPersonsByNameAsync(name, ct);
        return Ok(_mapper.Map<IEnumerable<PersonResponse>>(people));
    }

    /// <summary>
    /// Find people by role name
    /// Example: /api/people/queries/by-role?roleName=Administrator
    /// </summary>
    [HttpGet("by-role")]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> FindByRole(
        [FromQuery] string roleName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("RoleName parameter is required");

        var people = await _personQueryService.FindPersonsByRoleAsync(roleName, ct);
        return Ok(_mapper.Map<IEnumerable<PersonResponse>>(people));
    }

    /// <summary>
    /// Get person with roles by ID
    /// Example: /api/people/queries/{id}/with-roles
    /// </summary>
    [HttpGet("{id:guid}/with-roles")]
    public async Task<ActionResult<PersonResponse>> GetWithRoles(Guid id, CancellationToken ct = default)
    {
        var person = await _personQueryService.GetPersonWithRolesAsync(id, ct);
        if (person == null)
            return NotFound();

        return Ok(_mapper.Map<PersonResponse>(person));
    }

    /// <summary>
    /// Get total count of people
    /// Example: /api/people/queries/count
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount(CancellationToken ct = default)
    {
        var count = await _personQueryService.CountPersonsAsync(ct);
        return Ok(count);
    }

    /// <summary>
    /// Check if any people have a specific role
    /// Example: /api/people/queries/has-role?roleName=Manager
    /// </summary>
    [HttpGet("has-role")]
    public async Task<ActionResult<bool>> HasRole(
        [FromQuery] string roleName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("RoleName parameter is required");

        var hasRole = await _personQueryService.HasPersonsWithRoleAsync(roleName, ct);
        return Ok(hasRole);
    }
}
