using Telegram.Bot;
using Telegram.Bot.Types;

namespace PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;

public interface IEndStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
}