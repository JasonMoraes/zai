using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Dtos.Sensors;

public record RegisterSensorRequest
{
    // Properties default to null so an absent field fails [Required] instead of looking like an empty value.
    // The pattern rejects NUL characters, which PostgreSQL cannot store in text columns.
    [Required(ErrorMessage = "Pole jest wymagane.")]
    [MaxCharacters(FieldLimits.NameMaxLength)]
    [RegularExpression(@"^[^\x00]*$", ErrorMessage = "Pole nie może zawierać znaku NUL.")]
    public string Name { get; init; } = null!;

    [Required(ErrorMessage = "Pole jest wymagane.")]
    public int? SeriesId { get; init; }
}
