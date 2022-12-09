namespace PayForMeBot.DbDriver.Models;

public class UserTable
{
    public int UserTableId { get; set; }
    public string? TelegramId { get; set; }
    public long TeamId { get; set; }
    public string? SbpLink { get; set; }
}