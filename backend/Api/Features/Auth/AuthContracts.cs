namespace Api.Features.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
