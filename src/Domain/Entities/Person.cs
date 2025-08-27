using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Person
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _fullName = string.Empty;
        public string FullName 
        { 
            get => _fullName;
            set => _fullName = Guard.Against.NullOrEmpty(value, nameof(value));
        }

        public string? Phone { get; set; }

        // A person can have many roles. Roles are extensible and managed separately
        public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
    }
}
