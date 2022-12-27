using System.Globalization;

namespace TelegramBotService.Models;

public class Product
{
    private static string[] separators = { "\r\n", "\r", "\n", " " };

    public double TotalPrice { get; set; }

    public double Price { get; set; }

    public int Count { get; set; }

    public string? Name { get; set; }

    public static bool TryParse(string message, out Product product)
    {
        var splitMessage = message.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        if (splitMessage.Length > 2)
        {
            if (double.TryParse(
                    splitMessage[^1].Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var price)
                && int.TryParse(splitMessage[^2], out var count))
            {
                if (price > 0 && count > 0)
                {
                    product = new Product
                    {
                        Count = count,
                        Name = string.Join(" ", splitMessage.Take(splitMessage.Length - 2)),
                        Price = price,
                        TotalPrice = price
                    };

                    return true;
                }
            }
        }

        product = new Product();
        return false;
    }
}