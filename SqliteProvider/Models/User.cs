namespace SqliteProvider.Models;

public class User
{
    public long UserChatId { get; set; }
    public string? Username { get; set; }
    public Guid TeamId { get; set; }
    public string? TinkoffLink { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Stage { get; set; }
}