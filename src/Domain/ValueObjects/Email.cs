using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public class Email : IEquatable<Email>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(value));

            if (value.Length > 256)
                throw new ArgumentException("Email cannot exceed 256 characters", nameof(value));

            if (!EmailRegex.IsMatch(value))
                throw new ArgumentException("Email format is invalid", nameof(value));

            Value = value.ToLowerInvariant();
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj) => Equals(obj as Email);

        public bool Equals(Email? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Email? left, Email? right)
        {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Email? left, Email? right) => !(left == right);

        public static implicit operator string(Email email) => email.Value;
        public static implicit operator Email(string value) => new Email(value);
    }
}
