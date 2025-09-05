namespace Domain.ValueObjects
{
    public class PasswordHash : IEquatable<PasswordHash>
    {
        public string Value { get; }

        public PasswordHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Password hash cannot be empty", nameof(value));

            // Basic validation for hash format (BCrypt hashes are typically 60 characters)
            if (value.Length < 20)
                throw new ArgumentException("Password hash format is invalid", nameof(value));

            Value = value;
        }

        public override string ToString() => "***"; // Never expose the hash value in logs

        public override bool Equals(object? obj) => Equals(obj as PasswordHash);

        public bool Equals(PasswordHash? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(PasswordHash? left, PasswordHash? right)
        {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PasswordHash? left, PasswordHash? right) => !(left == right);

        public static implicit operator string(PasswordHash passwordHash) => passwordHash.Value;
        public static implicit operator PasswordHash(string value) => new PasswordHash(value);
    }
}
