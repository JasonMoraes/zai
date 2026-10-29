using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Api.Errors;

/// <summary>
/// Builds problem+json responses whose <c>errors</c> is an array of <c>{ field, message }</c>,
/// as the API contract requires (ASP.NET's default is a field → messages dictionary).
/// </summary>
public static class ValidationProblems
{
    public static ObjectResult Create(HttpContext httpContext, int statusCode, string title, IEnumerable<ValidationError> errors)
    {
        // The factory fills in type, status, title and traceId; copy them onto the contract's shape.
        var defaults = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(httpContext, statusCode, title);
        var problem = new ApiProblemDetails
        {
            Type = defaults.Type,
            Title = defaults.Title,
            Status = defaults.Status,
            Errors = errors.ToList(),
        };
        foreach (var (key, value) in defaults.Extensions)
            problem.Extensions[key] = value;

        return new ObjectResult(problem) { StatusCode = statusCode, ContentTypes = { "application/problem+json" } };
    }

    /// <summary>422: the request is well-formed but breaks a business rule on one field.</summary>
    public static ObjectResult Unprocessable(HttpContext httpContext, string field, string message) =>
        Create(httpContext, StatusCodes.Status422UnprocessableEntity, "Dane nie spełniają reguł.",
            [new ValidationError(field, message)]);

    /// <summary>Replacement for [ApiController]'s automatic 400 on invalid model state.</summary>
    public static IActionResult FromModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ValidationError(
                FieldName(entry.Key),
                string.IsNullOrEmpty(error.ErrorMessage) ? "Niepoprawna wartość." : error.ErrorMessage)));

        return Create(context.HttpContext, StatusCodes.Status400BadRequest, "Niepoprawne żądanie.", errors);
    }

    // Model state keys are "username", "$.username" (JSON reader errors) or "" / "$" (whole body).
    private static string FieldName(string key)
    {
        var name = key.StartsWith("$.") ? key[2..] : key;
        return name is "" or "$" ? "body" : name;
    }
}
