using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Configuration;

/// <summary>Credentials of the admin account created on first startup.</summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    [Required, MaxLength(FieldLimits.UsernameMaxLength)]
    public string Username { get; set; } = "";

    [Required, MinLength(FieldLimits.PasswordMinLength), MaxLength(FieldLimits.PasswordMaxLength)]
    public string Password { get; set; } = "";
}
