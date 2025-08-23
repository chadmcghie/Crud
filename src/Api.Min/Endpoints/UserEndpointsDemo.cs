using Api.Dto;

namespace Api.Endpoints
{
    public class UserEndpointsDemo
    {


        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users");

            // CREATE
            group.MapPost("/", (UserDto user) =>
            {
                // In a real app, call Application layer service/command
                return Results.Created($"/users/{user.Id}", user);
            });

            // READ ALL
            group.MapGet("/", () =>
            {
                var users = new List<UserDto>
            {
                new UserDto(Guid.NewGuid(), "Alice"),
                new UserDto(Guid.NewGuid(), "Bob")
            };
                return Results.Ok(users);
            });

            // READ ONE
            group.MapGet("/{id:guid}", (Guid id) =>
            {
                var user = new UserDto(id, "Demo User");
                return Results.Ok(user);
            });

            // UPDATE
            group.MapPut("/{id:guid}", (Guid id, UserDto updatedUser) =>
            {
                // In a real app, update via Application service
                return Results.NoContent();
            });

            // DELETE
            group.MapDelete("/{id:guid}", (Guid id) =>
            {
                // In a real app, delete via Application service
                return Results.NoContent();
            });
        }
    }
    
}