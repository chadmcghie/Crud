using System.Security.Cryptography;
using System.Text;
using Api.Dtos;
using Api.Services;
using App.Features.People;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;

namespace Api.Controllers;

[ApiController]
[Tags("People")]
[Route("api/[controller]")]
[Authorize]
public class PeopleController(IMediator mediator, IMapper mapper, IOutputCacheInvalidationService cacheInvalidation) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "PeoplePolicy")]
    public async Task<ActionResult<IEnumerable<PersonResponse>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListPeopleQuery(), ct);
        
        // Set Last-Modified header based on most recent entity timestamp
        if (items.Any())
        {
            var lastModified = items.Max(p => p.UpdatedAt ?? p.CreatedAt);
            Response.Headers.LastModified = lastModified.ToString("R");
        }
        
        return Ok(mapper.Map<IEnumerable<PersonResponse>>(items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<ActionResult<PersonResponse>> Get(Guid id, CancellationToken ct)
    {
        var p = await mediator.Send(new GetPersonQuery(id), ct);
        if (p is null)
            return NotFound();
            
        // Generate ETag and Last-Modified for conditional requests
        var responseDto = mapper.Map<PersonResponse>(p);
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseDto);
        var etag = GenerateETag(responseJson);
        var lastModified = p.UpdatedAt ?? p.CreatedAt;
        
        // Handle conditional requests
        var requestHeaders = Request.GetTypedHeaders();
        
        // Check If-None-Match (ETag)
        if (requestHeaders.IfNoneMatch?.Any(entityTag => entityTag.Tag == etag) == true)
        {
            return StatusCode(304); // Not Modified
        }
        
        // Check If-Modified-Since (Last-Modified)
        if (requestHeaders.IfModifiedSince.HasValue && 
            lastModified <= requestHeaders.IfModifiedSince.Value)
        {
            return StatusCode(304); // Not Modified
        }
        
        // Set response headers
        Response.Headers.ETag = etag;
        Response.Headers.LastModified = lastModified.ToString("R");
        
        return Ok(responseDto);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    {
        var p = await mediator.Send(new CreatePersonCommand(request.FullName, request.Phone, request.RoleIds), ct);

        // Invalidate collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("people", ct);

        return CreatedAtAction(nameof(Get), new { id = p.Id }, mapper.Map<PersonResponse>(p));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        await mediator.Send(new UpdatePersonCommand(id, request.FullName, request.Phone, request.RoleIds), ct);

        // Invalidate both entity and collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("people", id, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeletePersonCommand(id), ct);

        // Invalidate both entity and collection cache
        await cacheInvalidation.InvalidateEntityCacheAsync("people", id, ct);

        return NoContent();
    }

    private static string GenerateETag(string content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_')}\"";
    }
}
