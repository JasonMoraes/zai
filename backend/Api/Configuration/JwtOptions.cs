using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Used both to sign tokens and as the only algorithm accepted when validating them.</summary>
    public const string Algorithm = SecurityAlgorithms.HmacSha256;

    /// <summary>Tolerance for clock drift when validating lifetimes; a token is accepted until <c>exp</c> + this.</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    /// <summary>HMAC-SHA256 key; at least 32 characters.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "";

    [Range(60, 86400)]
    public int ExpiresInSeconds { get; set; } = 3600;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
