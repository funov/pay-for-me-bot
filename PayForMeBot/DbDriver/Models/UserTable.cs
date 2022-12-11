using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.DbDriver.Models;

public class UserTable
{
    [Key]
    public long UserChatId { get; set; }
    public string? Username { get; set; }
    public Guid TeamId { get; set; }
    public string? TinkoffLink { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? Stage { get; set; }
}