namespace Api.Entities;

/// <summary>A JWT invalidated by logout, identified by its <c>jti</c>; kept until the token would have expired anyway.</summary>
public class RevokedToken
{
    public const int JtiMaxLength = 64;

    public required string Jti { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
