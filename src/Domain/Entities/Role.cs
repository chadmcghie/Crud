using Ardalis.GuardClauses;

namespace Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            _name = Guard.Against.NullOrWhiteSpace(value, nameof(value));
            Guard.Against.StringTooLong(value, 100, nameof(value));
        }
    }

    private string? _description;
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

    public byte[]? RowVersion { get; set; }
}
