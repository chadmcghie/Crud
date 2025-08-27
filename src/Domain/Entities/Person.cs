using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Person
    {
        private string _fullName = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string FullName 
        { 
            get => _fullName;
            set => _fullName = Guard.Against.NullOrWhiteSpace(value, nameof(value));
        }

        public string? Phone { get; set; }

        // A person can have many roles. Roles are extensible and managed separately
        public ICollection<Role> Roles { get; set; } = new HashSet<Role>();

        // Concurrency token for optimistic concurrency control
        // Nullable for SQLite compatibility
        public byte[]? RowVersion { get; set; }

        public Person(string fullName)
        {
            FullName = fullName; // This will use the setter validation
        }

        // Parameterless constructor for EF Core and other frameworks
        public Person()
        {
        }
    }
}
