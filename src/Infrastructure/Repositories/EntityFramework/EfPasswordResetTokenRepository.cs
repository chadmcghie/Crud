using Domain.Entities.Authentication;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.EntityFramework;

public class EfPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EfPasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _context.PasswordResetTokens
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Token == token, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Get the most recent valid token for the user
        return await _context.PasswordResetTokens
            .Include(prt => prt.User)
            .Where(prt => prt.UserId == userId && !prt.IsUsed)
            .OrderByDescending(prt => prt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PasswordResetToken> AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        // Invalidate any existing unused tokens for the same user
        var existingTokens = await _context.PasswordResetTokens
            .Where(prt => prt.UserId == token.UserId && !prt.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.Expire();
        }

        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesWithRetryAsync(cancellationToken: cancellationToken);
        return token;
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        // Check if the entity is already tracked
        var local = _context.Set<PasswordResetToken>().Local.FirstOrDefault(prt => prt.Id == token.Id);

        if (local != null && local != token)
        {
            // Different instance is tracked, detach it
            _context.Entry(local).State = EntityState.Detached;
        }

        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesWithRetryAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Remove(token);
        await _context.SaveChangesWithRetryAsync(cancellationToken: cancellationToken);
    }

    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _context.PasswordResetTokens
            .Where(prt => prt.ExpiresAt < DateTime.UtcNow || prt.IsUsed)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Any())
        {
            _context.PasswordResetTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesWithRetryAsync(cancellationToken: cancellationToken);
        }

        return expiredTokens.Count;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesWithRetryAsync(cancellationToken: cancellationToken);
    }
}
