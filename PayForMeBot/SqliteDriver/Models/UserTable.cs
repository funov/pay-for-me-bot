namespace PayForMeBot.SqliteDriver.Models;

public class UserTable
{
    public string telegramId { get; set; } 
    public Guid teamId { get; set; }
    public string sbpLink { get; set; }
}