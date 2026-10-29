using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Dtos.Auth;

public record LoginRequest
{
    // Properties default to null (not "") so an absent field fails [Required] instead of looking like an empty value.
    // The pattern rejects NUL characters, which PostgreSQL cannot store in text columns.
    [Required(ErrorMessage = "Pole jest wymagane.")]
    [MaxCharacters(FieldLimits.UsernameMaxLength)]
    [RegularExpression(@"^[^\x00]*$", ErrorMessage = "Pole nie może zawierać znaku NUL.")]
    public string Username { get; init; } = null!;

    [Required(AllowEmptyStrings = true, ErrorMessage = "Pole jest wymagane.")]
    [MaxCharacters(FieldLimits.PasswordMaxLength)]
    public string Password { get; init; } = null!;
}
