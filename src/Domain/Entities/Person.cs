using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Person
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        // A person can have many roles. Roles are extensible and managed separately
        public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
    }
}
