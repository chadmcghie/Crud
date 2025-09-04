using Domain.Entities.Authentication;
using Domain.ValueObjects;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their ID
        /// </summary>
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their email address
        /// </summary>
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user with the given email exists
        /// </summary>
        Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new user to the repository
        /// </summary>
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user
        /// </summary>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user from the repository
        /// </summary>
        Task DeleteAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their refresh token
        /// </summary>
        Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves changes to the repository
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
