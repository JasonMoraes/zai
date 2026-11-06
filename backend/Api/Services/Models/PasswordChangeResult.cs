namespace Api.Services.Models;

public enum PasswordChangeResult
{
    Changed,
    UserNotFound,
    WrongCurrentPassword,
    NewPasswordTooShort,
    NewPasswordTooLong,
    NewPasswordSameAsCurrent,
}
