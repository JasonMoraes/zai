namespace Api.Services;

public interface IApiKeyGenerator
{
    /// <summary>Creates a new random key and its hash; only the hash may be stored.</summary>
    GeneratedApiKey Generate();

    /// <summary>Hashes a presented key so it can be looked up against stored hashes.</summary>
    string Hash(string apiKey);
}
