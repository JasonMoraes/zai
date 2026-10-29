using System.ComponentModel.DataAnnotations;

namespace Api.Dtos.Auth;

/// <summary>
/// Only presence is checked here (missing or null → 400). Empty and whitespace values are allowed through:
/// password rules are business rules checked by the service (422 for the new password, 403 for a wrong current one).
/// Properties default to null (not "") so an absent field fails [Required] instead of looking like an empty value.
/// </summary>
public record PasswordChangeRequest
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Pole jest wymagane.")]
    public string CurrentPassword { get; init; } = null!;

    [Required(AllowEmptyStrings = true, ErrorMessage = "Pole jest wymagane.")]
    public string NewPassword { get; init; } = null!;
}
