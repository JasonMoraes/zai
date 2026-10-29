using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

/// <summary>The contract's <c>Problem</c> schema: RFC 9457 problem details plus an <c>errors</c> array of <c>{ field, message }</c>.</summary>
public class ApiProblemDetails : ProblemDetails
{
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];
}
