using Api.Services.Models;

namespace Api.Dtos.Auth;

public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
