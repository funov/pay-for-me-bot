namespace SqliteProvider.Models;

public class UserProductBinding
{
    public Guid Id { get; set; }
    public long UserChatId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TeamId { get; set; }
}