namespace PayForMeBot.ReceiptApiClient.Models;

public class Receipt
{
    public Product[]? Products { get; set; }

    public string? ShopName { get; set; }

    public double TotalPriceSum { get; set; }
}