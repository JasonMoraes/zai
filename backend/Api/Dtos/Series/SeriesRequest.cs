using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Dtos.Series;

/// <summary>
/// Body of POST and PUT (a PUT replaces every editable field). Schema violations here give 400;
/// the cross-field rule minValue &lt; maxValue is a business rule checked by the service (422).
/// </summary>
public record SeriesRequest
{
    // Properties default to null so an absent field fails [Required] instead of looking like an empty value.
    // The NUL patterns reject characters PostgreSQL cannot store in text columns.
    [Required(ErrorMessage = "Pole jest wymagane.")]
    [MaxCharacters(FieldLimits.NameMaxLength)]
    [RegularExpression(@"^[^\x00]*$", ErrorMessage = "Pole nie może zawierać znaku NUL.")]
    public string Name { get; init; } = null!;

    [Required(ErrorMessage = "Pole jest wymagane.")]
    [FiniteNumber]
    public double? MinValue { get; init; }

    [Required(ErrorMessage = "Pole jest wymagane.")]
    [FiniteNumber]
    public double? MaxValue { get; init; }

    [Required(ErrorMessage = "Pole jest wymagane.")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Oczekiwano koloru w formacie #RRGGBB, np. #1f77b4.")]
    public string Color { get; init; } = null!;

    [RegularExpression(@"^[^\x00]*$", ErrorMessage = "Pole nie może zawierać znaku NUL.")]
    public string? Icon { get; init; }

    [MaxCharacters(FieldLimits.UnitMaxLength)]
    [RegularExpression(@"^[^\x00]*$", ErrorMessage = "Pole nie może zawierać znaku NUL.")]
    public string? Unit { get; init; }
}
