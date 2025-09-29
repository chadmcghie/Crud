using Ardalis.GuardClauses;

namespace Domain.Entities;

public class Role : BaseEntity
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        private set
        {
            _name = Guard.Against.NullOrWhiteSpace(value, nameof(value));
            Guard.Against.StringTooLong(value, 100, nameof(value));
        }
    }

    private string? _description;
    public string? Description
    {
        get => _description;
        private set
        {
            if (value != null)
            {
                Guard.Against.StringTooLong(value, 500, nameof(value));
            }
            _description = value;
        }
    }

    // EF Core constructor
    private Role()
    {
    }

    // Internal constructor for testing - allows setting Id for unit tests
    internal Role(Guid id, string name, string? description = null)
    {
        SetId(id);
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    // Factory method for creating new Role
    public static Role Create(string name, string? description = null)
    {
        return new Role
        {
            Name = name,
            Description = description
        };
    }

    // Factory method for testing with specific ID
    internal static Role CreateForTesting(Guid id, string name, string? description = null)
    {
        return new Role(id, name, description);
    }

    // Domain methods for state changes
    public void UpdateName(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        if (Name != name)
        {
            Name = name;
            MarkAsUpdated();
        }
    }

    public void UpdateDescription(string? description)
    {
        if (Description != description)
        {
            Description = description;
            MarkAsUpdated();
        }
    }
}
