namespace PayForMeBot.ReceiptApiClient.Models;

public class Product
{
    public int TotalPrice { get; set; }

    public int Price { get; set; }

    public int Count { get; set; }

    public string? Name { get; set; }
}