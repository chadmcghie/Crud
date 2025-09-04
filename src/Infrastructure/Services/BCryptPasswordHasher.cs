using Domain.Interfaces;

namespace Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
