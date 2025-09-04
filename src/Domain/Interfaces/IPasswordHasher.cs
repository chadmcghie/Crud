namespace Domain.Interfaces
{
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a plain text password
        /// </summary>
        /// <param name="password">The plain text password to hash</param>
        /// <returns>The hashed password</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plain text password against a hash
        /// </summary>
        /// <param name="password">The plain text password to verify</param>
        /// <param name="hashedPassword">The hashed password to verify against</param>
        /// <returns>True if the password matches the hash, false otherwise</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
