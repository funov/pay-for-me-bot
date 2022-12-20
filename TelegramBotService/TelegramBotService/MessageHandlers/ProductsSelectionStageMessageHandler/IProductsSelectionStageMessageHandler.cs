using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotService.TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;

public interface IProductsSelectionStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
    Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
    Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancellationToken);
}