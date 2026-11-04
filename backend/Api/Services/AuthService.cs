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
    // Verified against when the username is unknown, so both failure paths cost one real Argon2id verification.
    // An Argon2id hash (current parameters) of a random password that was thrown away, so nothing can match it.
    // If Argon2idPasswordHasher's parameters are ever raised, regenerate this so its cost keeps matching real hashes.
    private const string DummyPasswordHash =
        "$argon2id$v=19$m=19456,t=2,p=1$NF1UJP+993zFMm+IC3poCQ$fdt7GIdHvY4q32zOxo9JtT4pPdYIxBYkbsuTOMEnb5Y";

    public async Task<AccessToken?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        // No stored password can be empty (minimum 8). Rejecting it before any lookup makes it equally fast for every
        // account, whatever hash format that account still has.
        if (password.Length == 0)
        {
            logger.LogWarning("Failed login for {Username}: empty password", ForLog(username));
            return null;
        }

        var user = await db.AdminUsers.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        var result = hasher.VerifyHashedPassword(
            user ?? new AdminUser { Username = "" }, user?.PasswordHash ?? DummyPasswordHash, password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {Username}", ForLog(username));
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            await UpgradeHashAsync(user, password, cancellationToken);

        return tokens.CreateToken(user);
    }

    // Best effort: the password was correct, so a failure to store the stronger hash must not fail the login.
    // The old hash still works and the upgrade is retried at the next login.
    private async Task UpgradeHashAsync(AdminUser user, string password, CancellationToken cancellationToken)
    {
        try
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning(e, "Could not upgrade the password hash for {Username}", user.Username);
        }
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
