using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public interface IPaymentStageMessageHandler
{
    Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
}