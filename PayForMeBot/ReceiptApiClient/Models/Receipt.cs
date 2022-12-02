namespace PayForMeBot.ReceiptApiClient.Models;

public class Receipt
{
    public Product[]? Products { get; set; }

    public string? ShopName { get; set; }

    public int TotalPriceSum { get; set; }
}