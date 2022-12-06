namespace PayForMeBot.SqliteDriver.Models;

public class UserProductTable
{
    public Guid Id { get; set; }
    public string userTelegramId { get; set; }
    public Guid productId { get; set; }
    public Guid teamId { get; set; }
}