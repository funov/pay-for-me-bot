namespace PayForMeBot.ReceiptApiClient.Models;

public class Product
{
    public double TotalPrice { get; set; }

    public double Price { get; set; }

    public int Count { get; set; }

    public string? Name { get; set; }
    
    public static bool TryParse(string message, out Product product)
    {
        if (message.Split(" ").Length > 2)
        {
            if (double.TryParse(message.Split(" ")[message.Length - 1], out var price) &&
                int.TryParse(message.Split(" ")[message.Length - 2], out var count))
            {
                product = new Product()
                {
                    Count = count,
                    Name = message.Split(" ").Take(message.Length - 2).ToString(),
                    Price = price,
                    TotalPrice = count * price
                };

                return true;
            }
        }

        product = new Product();

        return false;
    }
}