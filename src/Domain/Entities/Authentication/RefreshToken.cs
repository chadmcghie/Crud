namespace Domain.Entities.Authentication
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        public bool IsActive => RevokedAt == null && !IsExpired;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;

        public RefreshToken(string token, DateTime expiresAt, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty", nameof(token));

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty", nameof(userId));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

            Id = Guid.NewGuid();
            Token = token;
            UserId = userId;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
            RevokedAt = null;
        }

        // EF Core constructor
        private RefreshToken()
        {
            Token = null!;
        }

        public void Revoke()
        {
            if (RevokedAt == null)
            {
                RevokedAt = DateTime.UtcNow;
            }
        }

        // Public factory method for testing with expired tokens
        public static RefreshToken CreateForTesting(string token, Guid userId, DateTime expiresAt)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                UserId = userId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            };
            return refreshToken;
        }
    }
}
