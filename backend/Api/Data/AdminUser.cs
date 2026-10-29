namespace Api.Data;

public class AdminUser
{
    public const int UsernameMaxLength = 100;


    public int Id { get; set; }
    public required string Username { get; set; }
    public string PasswordHash { get; set; } = "";
}
