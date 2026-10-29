namespace Api.Validation;

/// <summary>Length limits shared by the database schema, request DTOs, services and configuration.</summary>
public static class FieldLimits
{
    public const int UsernameMaxLength = 100;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 256;

    /// <summary>Series and sensor names (contract: 1–100 characters).</summary>
    public const int NameMaxLength = 100;
    public const int UnitMaxLength = 20;
    /// <summary>Series colour, "#RRGGBB".</summary>
    public const int ColorLength = 7;
}
