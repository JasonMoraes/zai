using System.Text.RegularExpressions;
using Api.Data;
using Api.Entities;
using Api.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public partial class AuthService(
    AppDbContext db,
    IPasswordHasher<AdminUser> hasher,
    ITokenService tokens,
    ITokenRevocationService revocations,
    ILogger<AuthService> logger) : IAuthService
{
    // Verified against when the username is unknown, so both failure paths take the same time.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<AdminUser>().HashPassword(new AdminUser { Username = "" }, Guid.NewGuid().ToString());

    public async Task<AccessToken?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await db.AdminUsers.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        var result = hasher.VerifyHashedPassword(
            user ?? new AdminUser { Username = "" }, user?.PasswordHash ?? DummyPasswordHash, password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {Username}", ForLog(username));
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync(cancellationToken);
        }

        return tokens.CreateToken(user);
    }

    public Task LogoutAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        revocations.RevokeAsync(jti, expiresAt, cancellationToken);

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        // Rules on the new password need no database, so they are checked first.
        // Length counts characters (Unicode code points, as JSON Schema does), not UTF-16 units.
        var length = newPassword.EnumerateRunes().Count();
        if (length < FieldLimits.PasswordMinLength)
            return PasswordChangeResult.NewPasswordTooShort;
        if (length > FieldLimits.PasswordMaxLength)
            return PasswordChangeResult.NewPasswordTooLong;
        if (newPassword == currentPassword)
            return PasswordChangeResult.NewPasswordSameAsCurrent;

        var user = await db.AdminUsers.FindAsync([userId], cancellationToken);
        if (user is null)
            return PasswordChangeResult.UserNotFound;

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed password change for {Username}: wrong current password", user.Username);
            return PasswordChangeResult.WrongCurrentPassword;
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password changed for {Username}", user.Username);

        return PasswordChangeResult.Changed;
    }

    // Strips control characters (no forged log lines) and truncates, in case a password was typed into the username field.
    private static string ForLog(string username)
    {
        var clean = ControlCharacters().Replace(username, "?");
        return clean.Length <= 32 ? clean : clean[..32] + "…";
    }

    [GeneratedRegex(@"\p{C}")]
    private static partial Regex ControlCharacters();
}
