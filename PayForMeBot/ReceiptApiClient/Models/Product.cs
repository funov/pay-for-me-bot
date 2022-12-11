using System.Globalization;

namespace PayForMeBot.ReceiptApiClient.Models;

public class Product
{
    public double TotalPrice { get; set; }

    public double Price { get; set; }

    public int Count { get; set; }

    public string? Name { get; set; }
    
    public static bool TryParse(string message, out Product product)
    {
        if (message.Split().Length > 2)
        {
            if (double.TryParse(message.Split()[message.Split().Length - 1].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                int.TryParse(message.Split()[message.Split().Length - 2], out var count))
            {
                product = new Product()
                {
                    Count = count,
                    Name = string.Join(" ", message.Split().Take(message.Split().Length - 2)),
                    Price = price,
                    TotalPrice = price
                };

                return true;
            }
        }

        product = new Product();

        return false;
    }
}