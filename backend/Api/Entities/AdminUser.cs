namespace Api.Entities;

public class AdminUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string PasswordHash { get; set; } = "";
}
