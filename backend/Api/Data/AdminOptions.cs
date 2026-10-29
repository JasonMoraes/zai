using System.ComponentModel.DataAnnotations;

namespace Api.Data;

/// <summary>Credentials of the admin account created on first startup.</summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    [Required, MaxLength(AdminUser.UsernameMaxLength)]
    public string Username { get; set; } = "";

    [Required, MinLength(12)]
    public string Password { get; set; } = "";
}
