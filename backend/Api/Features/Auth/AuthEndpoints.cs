using Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Auth;

public static class AuthEndpoints
{
    private const int PasswordMaxLength = 256;

    // Verified against when the username is unknown, so both failure paths take the same time.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<AdminUser>().HashPassword(new AdminUser { Username = "" }, Guid.NewGuid().ToString());

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("auth");

        auth.MapPost("/login", Login)
            .WithSummary("Logowanie administratora")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return api;
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, ProblemHttpResult>> Login(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher<AdminUser> hasher,
        TokenService tokens,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username))
            errors["username"] = ["Pole jest wymagane."];
        else if (request.Username.Length > AdminUser.UsernameMaxLength || request.Username.Contains('\0'))
            errors["username"] = [$"Pole może mieć maksymalnie {AdminUser.UsernameMaxLength} znaków i nie może zawierać znaku NUL."];
        if (string.IsNullOrEmpty(request.Password))
            errors["password"] = ["Pole jest wymagane."];
        else if (request.Password.Length > PasswordMaxLength)
            errors["password"] = [$"Pole może mieć maksymalnie {PasswordMaxLength} znaków."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var user = await db.AdminUsers.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        var result = hasher.VerifyHashedPassword(
            user ?? new AdminUser { Username = "" }, user?.PasswordHash ?? DummyPasswordHash, request.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            loggerFactory.CreateLogger(typeof(AuthEndpoints)).LogWarning("Failed login for {Username}", request.Username);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "Niepoprawna nazwa użytkownika lub hasło.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok(tokens.CreateToken(user));
    }
}
