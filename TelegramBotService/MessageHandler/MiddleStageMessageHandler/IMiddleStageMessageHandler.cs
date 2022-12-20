using Telegram.Bot;
using Telegram.Bot.Types;

namespace PayForMeBot.TelegramBotService.MessageHandler.MiddleStageMessageHandler;

public interface IMiddleStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
    Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
    Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancellationToken);
}