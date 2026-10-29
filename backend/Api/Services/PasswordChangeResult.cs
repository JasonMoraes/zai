namespace Api.Services;

public enum PasswordChangeResult
{
    Changed,
    UserNotFound,
    WrongCurrentPassword,
    NewPasswordTooShort,
    NewPasswordTooLong,
    NewPasswordSameAsCurrent,
}
