namespace ReceiptApiClient.Models;

public class Product
{
    public double TotalPrice { get; set; }

    public double Price { get; set; }

    public int Count { get; set; }

    public string? Name { get; set; }
}