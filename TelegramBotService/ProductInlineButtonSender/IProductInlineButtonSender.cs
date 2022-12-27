using Telegram.Bot;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.Models;

namespace TelegramBotService.ProductInlineButtonSender;

public interface IProductInlineButtonSender
{
    Task SendProductInlineButtonAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        IKeyboardMarkup keyboardMarkup,
        Product product,
        Guid productId,
        CancellationToken cancellationToken);

    Task SendProductsInlineButtonsAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        IEnumerable<Product> products,
        IEnumerable<Guid> productsIds,
        CancellationToken cancellationToken);
}