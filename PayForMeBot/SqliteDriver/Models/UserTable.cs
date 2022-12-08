namespace PayForMeBot.SqliteDriver.Models;

public class UserTable
{
    public int UserTableId { get; set; } 
    public string telegramId { get; set; } 
    public long teamId { get; set; }
    public string? sbpLink { get; set; }
}