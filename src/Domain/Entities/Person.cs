using Ardalis.GuardClauses;
using Domain.Exceptions;

namespace Domain.Entities
{
    public class Person : BaseEntity
    {
        private readonly HashSet<Role> _roles = new();

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            private set
            {
                _fullName = Guard.Against.NullOrWhiteSpace(value, nameof(value));
                Guard.Against.StringTooLong(value, 200, nameof(value));
            }
        }

        private string? _phone;
        public string? Phone
        {
            get => _phone;
            private set
            {
                if (value != null)
                {
                    ValidatePhone(value);
                }
                _phone = value;
            }
        }

        // A person can have many roles. Roles are extensible and managed separately
        public IReadOnlyCollection<Role> Roles => _roles;

        // EF Core constructor
        private Person()
        {
        }

        // Factory method for creating new Person
        public static Person Create(string fullName, string? phone = null)
        {
            return new Person
            {
                FullName = fullName,
                Phone = phone
            };
        }

        // Domain methods for state changes
        public void UpdateFullName(string fullName)
        {
            Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));

            if (FullName != fullName)
            {
                FullName = fullName;
                MarkAsUpdated();
            }
        }

        public void UpdatePhone(string? phone)
        {
            if (Phone != phone)
            {
                Phone = phone;
                MarkAsUpdated();
            }
        }

        public void AddRole(Role role)
        {
            Guard.Against.Null(role, nameof(role));

            if (_roles.Add(role))
            {
                MarkAsUpdated();
            }
        }

        public void RemoveRole(Role role)
        {
            Guard.Against.Null(role, nameof(role));

            if (_roles.Remove(role))
            {
                MarkAsUpdated();
            }
        }

        public void ClearRoles()
        {
            if (_roles.Count > 0)
            {
                _roles.Clear();
                MarkAsUpdated();
            }
        }

        public void UpdateRoles(IEnumerable<Role> newRoles)
        {
            Guard.Against.Null(newRoles, nameof(newRoles));

            var newRoleSet = new HashSet<Role>(newRoles);

            // Only update if roles actually changed
            if (!_roles.SetEquals(newRoleSet))
            {
                // Use more EF Core-friendly approach for many-to-many updates
                // Remove roles that are no longer needed
                var rolesToRemove = _roles.Except(newRoleSet).ToList();
                foreach (var role in rolesToRemove)
                {
                    _roles.Remove(role);
                }

                // Add new roles that weren't already present
                var rolesToAdd = newRoleSet.Except(_roles).ToList();
                foreach (var role in rolesToAdd)
                {
                    _roles.Add(role);
                }

                MarkAsUpdated();
            }
        }

        private static void ValidatePhone(string phone)
        {
            Guard.Against.StringTooLong(phone, 20, nameof(phone));

            // Basic phone validation - could be enhanced with more sophisticated rules
            if (phone.Length > 0 && !phone.All(c => char.IsDigit(c) || c == '-' || c == '(' || c == ')' || c == ' ' || c == '+'))
            {
                throw new DomainException("Phone number contains invalid characters");
            }
        }
    }
}
