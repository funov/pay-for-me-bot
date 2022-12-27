using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.Models;

namespace TelegramBotService.ProductInlineButtonSender;

public class ProductInlineButtonSender : IProductInlineButtonSender
{
    private readonly ILogger<ProductInlineButtonSender> log;
    private readonly IKeyboardMarkup keyboardMarkup;

    public ProductInlineButtonSender(ILogger<ProductInlineButtonSender> log, IKeyboardMarkup keyboardMarkup)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
    }

    public async Task SendProductInlineButtonAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        IKeyboardMarkup keyboardMarkup,
        Product product,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
            productId,
            $"{product.TotalPrice} р.",
            $"{product.Count} шт.",
            "🛒");

        log.LogInformation("Send product {ProductId} inline button to @{username} in chat {ChatId}",
            productId, userName, chatId);

        await client.SendTextMessageAsync(
            chatId,
            product.Name!,
            replyMarkup: inlineKeyboardMarkup,
            parseMode: ParseMode.Html,
            disableNotification: true,
            cancellationToken: cancellationToken);
    }

    public async Task SendProductsInlineButtonsAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        IEnumerable<Product> products,
        IEnumerable<Guid> productsIds,
        CancellationToken cancellationToken)
    {
        var productsArray = products.ToArray();
        var idsArray = productsIds.ToArray();

        if (productsArray.Length != idsArray.Length)
        {
            throw new ArgumentException("Product ids count not equal products count");
        }

        for (var i = 0; i < idsArray.Length; i++)
        {
            await SendProductInlineButtonAsync(client, chatId, userName, keyboardMarkup, productsArray[i], idsArray[i],
                cancellationToken);
        }
    }
}