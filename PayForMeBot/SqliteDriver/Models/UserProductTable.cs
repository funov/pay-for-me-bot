using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.SqliteDriver.Models;

public class UserProductTable
{
    [Key]
    public string? userTelegramId { get; set; }
    public Guid productId { get; set; }
    public long teamId { get; set; }
}