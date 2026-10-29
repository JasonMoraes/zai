namespace Api.Errors;

/// <summary>One entry of the <c>errors</c> array in a problem+json response.</summary>
public record ValidationError(string Field, string Message);
