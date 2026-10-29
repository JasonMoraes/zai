namespace Api.Services;

public interface IAuthService
{
    /// <summary>Returns a token for valid admin credentials, or <c>null</c> when they are wrong.</summary>
    Task<AccessToken?> LoginAsync(string username, string password, CancellationToken cancellationToken);

    /// <summary>Invalidates the token with the given <c>jti</c> until it expires.</summary>
    Task LogoutAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    Task<PasswordChangeResult> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
