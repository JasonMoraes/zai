using Api.Dtos.Auth;
using Api.Errors;
using Api.Extensions;
using Api.Services;
using Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [EndpointSummary("Logowanie administratora")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(request.Username, request.Password, cancellationToken);
        if (token is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Niepoprawna nazwa użytkownika lub hasło.");

        return new LoginResponse(token.Value, "Bearer", token.ExpiresInSeconds);
    }

    [Authorize]
    [HttpPost("logout")]
    [EndpointSummary("Wylogowanie (unieważnienie tokenu)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Validated tokens always carry both (a jti is enforced on validation, exp by lifetime validation).
        if (User.GetTokenId() is not { } jti || User.GetTokenExpiry() is not { } expiresAt)
            return Challenge();

        await authService.LogoutAsync(jti, expiresAt, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("password")]
    [EndpointSummary("Zmiana hasła zalogowanego administratora")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
            return Challenge();

        var result = await authService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        return result switch
        {
            PasswordChangeResult.Changed => NoContent(),
            PasswordChangeResult.UserNotFound => Challenge(),
            PasswordChangeResult.WrongCurrentPassword => Problem(
                statusCode: StatusCodes.Status403Forbidden, detail: "Podane bieżące hasło jest niepoprawne."),
            PasswordChangeResult.NewPasswordTooShort => NewPasswordProblem(
                $"Hasło musi mieć co najmniej {FieldLimits.PasswordMinLength} znaków."),
            PasswordChangeResult.NewPasswordTooLong => NewPasswordProblem(
                $"Hasło może mieć maksymalnie {FieldLimits.PasswordMaxLength} znaków."),
            PasswordChangeResult.NewPasswordSameAsCurrent => NewPasswordProblem(
                "Nowe hasło musi różnić się od bieżącego."),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };
    }

    private ObjectResult NewPasswordProblem(string message) =>
        ValidationProblems.Unprocessable(HttpContext, "newPassword", message);
}
