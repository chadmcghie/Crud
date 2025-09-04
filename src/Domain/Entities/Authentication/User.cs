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
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public bool IsLocked { get; private set; }
        public IReadOnlyCollection<string> Roles => _roles;
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Concurrency token for optimistic concurrency control
        // Nullable for SQLite compatibility
        public byte[]? RowVersion { get; set; }

        public User(Email email, PasswordHash passwordHash, string? firstName = null, string? lastName = null)
        {
            Id = Guid.NewGuid();
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            FirstName = firstName;
            LastName = lastName;
            IsLocked = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            
            // Default role for new users
            _roles.Add("User");
        }
        
        public static User Create(Email email, PasswordHash passwordHash, string? firstName = null, string? lastName = null)
        {
            return new User(email, passwordHash, firstName, lastName);
        }

        // EF Core constructor
        private User()
        {
            Email = null!;
            PasswordHash = null!;
        }

        public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
        {
            var refreshToken = new RefreshToken(token, expiresAt, Id);
            _refreshTokens.Add(refreshToken);
            UpdatedAt = DateTime.UtcNow;
            return refreshToken;
        }
        
        public void AddRefreshToken(RefreshToken refreshToken)
        {
            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));
            _refreshTokens.Add(refreshToken);
            UpdatedAt = DateTime.UtcNow;
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

        public void RevokeAllRefreshTokens()
        {
            foreach (var refreshToken in _refreshTokens.Where(rt => rt.IsActive))
            {
                refreshToken.Revoke();
            }
            UpdatedAt = DateTime.UtcNow;
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
        
        public void LockAccount()
        {
            IsLocked = true;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void UnlockAccount()
        {
            IsLocked = false;
            UpdatedAt = DateTime.UtcNow;
        }

        // Internal method for testing purposes only
        internal void AddRefreshTokenForTesting(RefreshToken refreshToken)
        {
            _refreshTokens.Add(refreshToken);
        }
    }
}