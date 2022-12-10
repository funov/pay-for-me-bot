using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;

public class EndStageMessageHandler : IEndStageMessageHandler
{
    private static HashSet<string> closeTeamFlags = new() {"/end", "end", "Готово"};

    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    private static HashSet<string> helpFlags = new() {"/help", "help", "Помощь"};

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

        if (closeTeamFlags.Contains(message.Text!))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Test closing team / Cкинь свои реквизиты",
                cancellationToken: cancellationToken);
            return;
        }

        if (CheckIfReceiveMoneyMethodIsValid(message.Text!))
        {
            // db.AddReceiveMoneyMethod(...);
            log.LogInformation("User sent valid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ждем остальных участников. Как только все отправят, " +
                      "я рассчитаю чеки и вышлю реквизиты",
                cancellationToken: cancellationToken);
        }

        else if (!CheckIfReceiveMoneyMethodIsValid(message.Text!))
        {
            log.LogInformation("User sent invalid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ты скинул неправильные реквизиты. " +
                      "Отправь мне ссылку на Тинькофф и/или номер телефона в формате +7",
                cancellationToken: cancellationToken);
        }

        if (helpFlags.Contains(message.Text!))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: HelpMessage,
                cancellationToken: cancellationToken);
        }
    }

    public async Task SendReceiveMoneyMethodsAndDebt(ITelegramBotClient client, Message message,
        CancellationToken cancellationToken)
    {
        // TODO сделать отправку реквизитов + общую стоимость
        // TODO видимо, нужно уметь ходить в бд

        var chatId = message.Chat.Id;
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: "rm method + total price",
            cancellationToken: cancellationToken
        );
    }

    private bool CheckIfReceiveMoneyMethodIsValid(string text)
    {
        // TODO проверить реквизиты на валидность. Номер телефона и/или ссылка на тиньк

        return true;
    }
}