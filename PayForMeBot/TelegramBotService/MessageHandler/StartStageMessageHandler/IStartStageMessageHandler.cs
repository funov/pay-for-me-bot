using Telegram.Bot;
using Telegram.Bot.Types;

namespace PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;

public interface IStartStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
}