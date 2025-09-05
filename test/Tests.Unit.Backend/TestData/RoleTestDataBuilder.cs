using Domain.Entities;

namespace Tests.Unit.Backend.TestData;

public class RoleTestDataBuilder
{
    private string _name = "Administrator";
    private string? _description = "System administrator role";

    public RoleTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleTestDataBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public Role Build()
    {
        return new Role
        {
            Name = _name,
            Description = _description
        };
    }

    public static RoleTestDataBuilder Default() => new();
}

