using App.Features.Roles;
using Domain.Entities;

namespace Tests.Unit.Backend;

public class MediatRCommandsQueriesTests
{
    [Fact]
    public void CreateRoleCommand_ShouldHaveCorrectProperties()
    {
        // Arrange
        var name = "Test Role";
        var description = "Test Description";

        // Act
        var command = new CreateRoleCommand(name, description);

        // Assert
        Assert.Equal(name, command.Name);
        Assert.Equal(description, command.Description);
    }

    [Fact]
    public void UpdateRoleCommand_ShouldHaveCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Updated Role";
        var description = "Updated Description";

        // Act
        var command = new UpdateRoleCommand(id, name, description);

        // Assert
        Assert.Equal(id, command.Id);
        Assert.Equal(name, command.Name);
        Assert.Equal(description, command.Description);
    }

    [Fact]
    public void DeleteRoleCommand_ShouldHaveCorrectId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var command = new DeleteRoleCommand(id);

        // Assert
        Assert.Equal(id, command.Id);
    }

    [Fact]
    public void GetRoleQuery_ShouldHaveCorrectId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var query = new GetRoleQuery(id);

        // Assert
        Assert.Equal(id, query.Id);
    }

    [Fact]
    public void ListRolesQuery_ShouldBeCreatable()
    {
        // Act
        var query = new ListRolesQuery();

        // Assert
        Assert.NotNull(query);
    }
}