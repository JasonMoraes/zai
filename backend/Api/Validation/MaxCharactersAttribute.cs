using System.ComponentModel.DataAnnotations;

namespace Api.Validation;

/// <summary>
/// Maximum length in characters (Unicode code points), as JSON Schema's <c>maxLength</c> and PostgreSQL's
/// <c>varchar(n)</c> count them. <see cref="MaxLengthAttribute"/> counts UTF-16 units, so an emoji counts twice.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxCharactersAttribute(int length) : ValidationAttribute("Pole może mieć maksymalnie {1} znaków.")
{
    public int Length { get; } = length;

    public override bool IsValid(object? value) => value is not string text || text.EnumerateRunes().Count() <= Length;

    public override string FormatErrorMessage(string name) => string.Format(ErrorMessageString, name, Length);
}
