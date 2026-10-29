namespace Api.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
}
