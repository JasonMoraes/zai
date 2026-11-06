using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Api.Entities;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

/// <summary>
/// Argon2id password hashing (course requirement T5.1; OWASP's first choice), stored in the standard PHC string
/// format: <c>$argon2id$v=19$m=19456,t=2,p=1$&lt;salt&gt;$&lt;hash&gt;</c>.
/// </summary>
public class Argon2idPasswordHasher : IPasswordHasher<AdminUser>
{
    // OWASP Password Storage Cheat Sheet minimum for Argon2id: 19 MiB of memory, 2 iterations, 1 degree of parallelism.
    private const int MemoryKib = 19456;
    private const int Iterations = 2;
    private const int Parallelism = 1;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Prefix = "$argon2id$v=19$";

    // Upper bound accepted when verifying a stored hash, so a tampered string can't make one verification allocate gigabytes.
    private const int MaxMemoryKib = 256 * 1024;

    public string HashPassword(AdminUser user, string password)
    {
        // Callers enforce a minimum length; an empty password reaching this point is a bug, not user input.
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Compute(password, salt, MemoryKib, Iterations, Parallelism, HashBytes);
        return string.Create(CultureInfo.InvariantCulture,
            $"{Prefix}m={MemoryKib},t={Iterations},p={Parallelism}${Encode(salt)}${Encode(hash)}");
    }

    public PasswordVerificationResult VerifyHashedPassword(AdminUser user, string hashedPassword, string providedPassword)
    {
        // The Argon2 library rejects empty input, and no stored password can be empty (minimum length 8), so an empty
        // attempt simply doesn't match. Failing fast here reveals nothing: it fails the same way for every account.
        if (providedPassword.Length == 0
            || !TryParse(hashedPassword, out var memory, out var iterations, out var parallelism, out var salt, out var expected))
            return PasswordVerificationResult.Failed;

        var actual = Compute(providedPassword, salt, memory, iterations, parallelism, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKib, int iterations, int parallelism, int length)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKib,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(length);
    }

    // "$argon2id$v=19$m=19456,t=2,p=1$<salt>$<hash>". Anything malformed, or parameters far outside what this class
    // writes (which could make verification allocate huge amounts of memory), is treated as a failed match.
    private static bool TryParse(
        string phc, out int memory, out int iterations, out int parallelism, out byte[] salt, out byte[] hash)
    {
        memory = iterations = parallelism = 0;
        salt = hash = [];

        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[0] != "" || parts[1] != "argon2id" || parts[2] != "v=19")
            return false;

        var parameters = parts[3].Split(',');
        if (parameters.Length != 3
            || !TryReadParameter(parameters[0], "m=", out memory) || memory is < 8 or > MaxMemoryKib
            || !TryReadParameter(parameters[1], "t=", out iterations) || iterations is < 1 or > 100
            || !TryReadParameter(parameters[2], "p=", out parallelism) || parallelism is < 1 or > 64
            || memory < 8 * parallelism) // Argon2 needs at least 8 KiB per lane
            return false;

        return TryDecode(parts[4], out salt) && salt.Length >= 8
            && TryDecode(parts[5], out hash) && hash.Length >= 16;
    }

    private static bool TryReadParameter(string text, string name, out int value)
    {
        value = 0;
        return text.StartsWith(name, StringComparison.Ordinal)
            && int.TryParse(text.AsSpan(name.Length), NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    // PHC strings use standard base64 without padding.
    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

    private static bool TryDecode(string text, out byte[] bytes)
    {
        bytes = [];
        var padded = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
        var buffer = new byte[padded.Length * 3 / 4];
        if (!Convert.TryFromBase64String(padded, buffer, out var written))
            return false;

        bytes = buffer[..written];
        return true;
    }
}