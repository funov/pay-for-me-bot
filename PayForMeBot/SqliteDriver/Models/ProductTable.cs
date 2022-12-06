using System.ComponentModel.DataAnnotations;

namespace PayForMeBot.SqliteDriver.Models;

public class ProductTable
{
    [Key]
    public Guid Id { get; set; }
    public  string? name { get; set; }
    public double totalPrice { get; set; }
    public Guid teamId { get; set; }
    public Guid receiptId { get; set; }
    public string buyerTelegramId  { get; set; }
}