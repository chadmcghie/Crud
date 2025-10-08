using System.Security.Cryptography;

namespace Domain.Entities.Authentication;

public class PasswordResetToken
{
    private const int TokenLength = 32; // 32 bytes = 256 bits
    private const int DefaultExpirationHours = 1;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Concurrency token for optimistic concurrency control
    // Nullable for SQLite compatibility
    public byte[]? RowVersion { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;

    private PasswordResetToken(Guid userId, string token, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsUsed = false;
        UsedAt = null;
        CreatedAt = DateTime.UtcNow;
    }

    // EF Core constructor
    private PasswordResetToken()
    {
        Token = null!;
    }

    /// <summary>
    /// Creates a new password reset token with the specified expiration time
    /// </summary>
    public static PasswordResetToken Create(Guid userId, DateTime? expiresAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        var expiration = expiresAt ?? DateTime.UtcNow.AddHours(DefaultExpirationHours);

        if (expiration <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        var token = GenerateSecureToken();
        return new PasswordResetToken(userId, token, expiration);
    }

    /// <summary>
    /// Marks the token as used, preventing future use
    /// </summary>
    public void MarkAsUsed()
    {
        if (!IsUsed)
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Forces the token to expire immediately
    /// </summary>
    public void Expire()
    {
        ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
    }

    /// <summary>
    /// Validates if the provided token string matches this token and is valid
    /// Uses constant-time comparison to prevent timing attacks
    /// </summary>
    public bool ValidateToken(string tokenToValidate)
    {
        if (string.IsNullOrWhiteSpace(tokenToValidate))
            return false;

        if (!IsValid)
            return false;

        return ConstantTimeEquals(Token, tokenToValidate);
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[TokenLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Convert to URL-safe base64 string
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        uint diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= (uint)(a[i] ^ b[i]);
        }
        return diff == 0;
    }

    /// <summary>
    /// Factory method for testing with specific token and expiration
    /// </summary>
    public static PasswordResetToken CreateForTesting(Guid userId, string token, DateTime expiresAt)
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsUsed = false,
            UsedAt = null,
            CreatedAt = DateTime.UtcNow
        };
        return resetToken;
    }
}
