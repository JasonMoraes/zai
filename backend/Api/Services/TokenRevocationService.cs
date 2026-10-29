using Api.Configuration;
using Api.Data;
using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class TokenRevocationService(AppDbContext db, TimeProvider time) : ITokenRevocationService
{
    public async Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        // Rows can go once their token is rejected for being expired — which is only after the clock skew too.
        var cutoff = time.GetUtcNow() - JwtOptions.ClockSkew;
        await db.RevokedTokens.Where(t => t.ExpiresAt < cutoff).ExecuteDeleteAsync(cancellationToken);

        db.RevokedTokens.Add(new RevokedToken { Jti = jti, ExpiresAt = expiresAt });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent logout with the same token already revoked it — the outcome we wanted.
        }
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) =>
        db.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
}
