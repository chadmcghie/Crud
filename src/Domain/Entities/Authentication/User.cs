using Domain.ValueObjects;

namespace Domain.Entities.Authentication
{
    public class User
    {
        private readonly List<RefreshToken> _refreshTokens = new();
        private readonly HashSet<string> _roles = new();

        public Guid Id { get; private set; }
        public Email Email { get; private set; }
        public PasswordHash PasswordHash { get; private set; }
        public IReadOnlyCollection<string> Roles => _roles;
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Concurrency token for optimistic concurrency control
        // Nullable for SQLite compatibility
        public byte[]? RowVersion { get; set; }

        public User(Email email, PasswordHash passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            
            // Default role for new users
            _roles.Add("User");
        }

        // EF Core constructor
        private User()
        {
            Email = null!;
            PasswordHash = null!;
        }

        public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
        {
            var refreshToken = new RefreshToken(token, Id, expiresAt);
            _refreshTokens.Add(refreshToken);
            UpdatedAt = DateTime.UtcNow;
            return refreshToken;
        }

        public bool RevokeRefreshToken(string token)
        {
            var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
            if (refreshToken == null)
                return false;

            refreshToken.Revoke();
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public RefreshToken? GetActiveRefreshToken(string token)
        {
            var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
            return refreshToken?.IsActive == true ? refreshToken : null;
        }

        public void AddRole(string role)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                _roles.Add(role);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void RemoveRole(string role)
        {
            _roles.Remove(role);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePassword(PasswordHash newPasswordHash)
        {
            PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
            UpdatedAt = DateTime.UtcNow;
        }

        public int CleanupExpiredTokens()
        {
            var expiredTokens = _refreshTokens.Where(rt => rt.IsExpired).ToList();
            foreach (var token in expiredTokens)
            {
                _refreshTokens.Remove(token);
            }
            
            if (expiredTokens.Any())
            {
                UpdatedAt = DateTime.UtcNow;
            }
            
            return expiredTokens.Count;
        }

        // Internal method for testing purposes only
        internal void AddRefreshTokenForTesting(RefreshToken refreshToken)
        {
            _refreshTokens.Add(refreshToken);
        }
    }
}