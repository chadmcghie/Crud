using System.Security.Cryptography;
using System.Text;
using Api.Dtos;
using Api.Services;
using App.Abstractions;
using App.Features.Roles;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;

namespace Api.Controllers;

[ApiController]
[Tags("Roles")]
[Route("api/[controller]")]
[Authorize]
public class RolesController(IMediator mediator, IMapper mapper, IOutputCacheInvalidationService cacheInvalidation) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new ListRolesQuery(), ct);
        var responseDto = mapper.Map<IEnumerable<RoleDto>>(items);

        // Generate ETag and Last-Modified for conditional requests
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseDto);
        var etag = GenerateETag(responseJson);

        DateTime? lastModified = null;
        if (items.Any())
        {
            lastModified = items.Max(r => r.UpdatedAt ?? r.CreatedAt);
        }

        // Handle conditional requests
        var requestHeaders = Request.GetTypedHeaders();

        // Check If-None-Match (ETag)
        if (requestHeaders.IfNoneMatch?.Any(entityTag =>
            entityTag.Tag == etag || entityTag.Tag == etag.Trim('"')) == true)
        {
            return StatusCode(304); // Not Modified
        }

        // Check If-Modified-Since (Last-Modified)
        if (requestHeaders.IfModifiedSince.HasValue &&
            lastModified.HasValue)
        {
            // Truncate both timestamps to second precision for proper HTTP date comparison
            var lastModifiedSeconds = new DateTime(lastModified.Value.Year, lastModified.Value.Month, lastModified.Value.Day,
                lastModified.Value.Hour, lastModified.Value.Minute, lastModified.Value.Second, DateTimeKind.Utc);
            var ifModifiedSinceSeconds = new DateTime(requestHeaders.IfModifiedSince.Value.Year, requestHeaders.IfModifiedSince.Value.Month, requestHeaders.IfModifiedSince.Value.Day,
                requestHeaders.IfModifiedSince.Value.Hour, requestHeaders.IfModifiedSince.Value.Minute, requestHeaders.IfModifiedSince.Value.Second, DateTimeKind.Utc);

            if (lastModifiedSeconds <= ifModifiedSinceSeconds)
            {
                return StatusCode(304); // Not Modified
            }
        }

        // Set response headers
        Response.Headers.ETag = etag;
        if (lastModified.HasValue)
        {
            Response.Headers.LastModified = lastModified.Value.ToString("R");
        }

        return Ok(responseDto);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")]
    [OutputCache(PolicyName = "RolesPolicy")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
    {
        var r = await mediator.Send(new GetRoleQuery(id), ct);
        if (r is null)
            return NotFound();

        // Set Last-Modified header based on entity timestamp
        var lastModified = r.UpdatedAt ?? r.CreatedAt;
        Response.Headers.LastModified = lastModified.ToString("R");

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

    private static string GenerateETag(string content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_')}\"";
    }
}
