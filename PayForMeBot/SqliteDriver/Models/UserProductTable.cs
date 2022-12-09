using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.SqliteDriver.Models;

public class UserProductTable
{
    [Key] 
    public string? UserTelegramId { get; set; }
    public Guid ProductId { get; set; }
    public long TeamId { get; set; }
}