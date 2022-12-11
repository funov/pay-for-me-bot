using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;

public class EndStageMessageHandler : IEndStageMessageHandler
{
    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    private static string HelpMessage
        => "❓❓❓\n\n1) Для начала нужно либо создать команду, либо вступить в существующую. 🤝🤝🤝\n\n" +
           "2) Далее каждого попросят ввести номер телефона и ссылку Тинькофф (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт и его цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар, если все хорошо, ты увидишь «✅», для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «Завершить». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);

        switch (message.Text!)
        {
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: HelpMessage,
                    cancellationToken: cancellationToken);
                return;
            case "Готово":
                if (!IsUserSentRequisite())
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Отправь свои реквизиты: " +
                              "номер телефона и/или ссылку на Тинькофф, если все в команде используют Тинькофф банк",
                        cancellationToken: cancellationToken);
                    return;
                }

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты уже нажал Готово, твои реквизиты приняты",
                    cancellationToken: cancellationToken);
                return;
        }

        if (IsRequisiteValid(message.Text!))
        {
            // db.AddReceiveMoneyMethod(...);
            log.LogInformation("User sent valid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ждем остальных участников. Как только все отправят, " +
                      "я рассчитаю чеки и вышлю реквизиты",
                cancellationToken: cancellationToken);
        }
        else
        {
            log.LogInformation("User sent invalid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ты скинул неправильные реквизиты. " +
                      "Отправь мне ссылку на Тинькофф и/или номер телефона в формате +7",
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendRequisitesAndDebts(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken)
    {
        // TODO Запрос в бд

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: "Ты должен ... рублей!",
            cancellationToken: cancellationToken
        );
    }

    private bool IsUserSentRequisite()
    {
        // TODO Уметь ходить в базу, проверять, отправил ли свои реквизиты пользователь
        return false;
    }

    private bool IsRequisiteValid(string text)
    {
        // TODO проверить реквизиты на валидность. Номер телефона и/или ссылка на тиньк
        return true;
    }
}