using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.DbDriver.Models;

public class UserProductTable
{
    [Key]
    public Guid Id { get; set; }
    public long UserChatId { get; set; }
    public Guid ProductId { get; set; }
    public Guid TeamId { get; set; }
}