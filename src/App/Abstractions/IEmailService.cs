namespace App.Abstractions;

public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email to the specified email address
    /// </summary>
    /// <param name="email">The recipient's email address</param>
    /// <param name="resetToken">The password reset token to include in the email</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}
