using Ardalis.GuardClauses;

namespace Domain.Entities;

public class Role
{
    private string _name = string.Empty;
    private string? _description;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name 
    { 
        get => _name;
        set
        {
            Guard.Against.NullOrWhiteSpace(value, nameof(value));
            Guard.Against.StringTooLong(value, 100, nameof(value));
            _name = value;
        }
    }

    public string? Description 
    { 
        get => _description;
        set
        {
            if (value != null)
            {
                Guard.Against.StringTooLong(value, 500, nameof(value));
            }
            _description = value;
        }
    }

    public Role(string name, string? description = null)
    {
        Name = name; // This will use the setter validation
        Description = description; // This will use the setter validation
    }

    // Parameterless constructor for EF Core and other frameworks
    public Role()
    {
    }
}
