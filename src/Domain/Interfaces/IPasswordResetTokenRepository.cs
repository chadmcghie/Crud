using Domain.Entities.Authentication;

namespace Domain.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PasswordResetToken> AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task DeleteAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
