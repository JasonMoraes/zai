using System.Security.Cryptography;
using System.Text;
using Api.Services.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Services;

/// <summary>
/// 256-bit random keys, stored as SHA-256. A fast unsalted hash is the right tool here (unlike passwords):
/// the key is unguessable, and a deterministic hash lets uploads find their sensor with an indexed lookup.
/// </summary>
public class ApiKeyGenerator : IApiKeyGenerator
{
    private const int KeyBytes = 32;

    public GeneratedApiKey Generate()
    {
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(KeyBytes));
        return new GeneratedApiKey(key, Hash(key));
    }

    public string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey))).ToLowerInvariant();
}
