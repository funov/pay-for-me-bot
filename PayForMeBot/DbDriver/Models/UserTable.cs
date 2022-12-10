using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.DbDriver.Models;

public class UserTable
{
    [Key]
    public string? UserTelegramId { get; set; } // chatId
    public Guid TeamId { get; set; }
    public string? SbpLink { get; set; }
    public string? Stage { get; set; }
}