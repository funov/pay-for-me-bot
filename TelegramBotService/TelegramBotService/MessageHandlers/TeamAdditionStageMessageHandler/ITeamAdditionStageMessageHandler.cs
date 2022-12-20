using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotService.TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

public interface ITeamAdditionStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
}