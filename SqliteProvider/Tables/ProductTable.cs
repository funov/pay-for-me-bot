using System.ComponentModel.DataAnnotations;

namespace SqliteProvider.Tables;

public class ProductTable
{
    [Key] 
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int Count { get; set; }
    public double Price { get; set; }
    public double TotalPrice { get; set; }
    public Guid TeamId { get; set; }
    public Guid ReceiptId { get; set; }
    public long BuyerChatId { get; set; }
}