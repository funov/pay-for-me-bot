using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;

public class StartStageMessageHandler : IStartStageMessageHandler
{
    private static HashSet<string> openTeamFlags = new() {"/start", "start", "Начать"};
    private static string[] teamSelectionLabels = {"Создать команду", "Присоединиться к команде"};

    private static HashSet<string> helpFlags = new() {"/help", "help", "Помощь"};

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

    public StartStageMessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IReceiptApiClient receiptApiClient,
        IKeyboardMarkup keyboardMarkup, IDbDriver dbDriver)
    {
        this.log = log;
        this.receiptApiClient = receiptApiClient;
        this.keyboardMarkup = keyboardMarkup;
        this.dbDriver = dbDriver;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);

        if (openTeamFlags.Contains(message.Text!))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Создай или присоединись к команде!",
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                cancellationToken: cancellationToken);
            return;
        }

        if (helpFlags.Contains(message.Text!))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: HelpMessage,
                cancellationToken: cancellationToken);
        }

        if (Guid.TryParse(message.Text, out var teamId))
        {
            log.LogInformation("{username} joined team {guid} in {chatId}",
                message.Chat.Username, teamId, chatId);
        }

        switch (message.Text!)
        {
            // TODO Брать их из массива (teamSelectionLabels)

            // TODO Подсчитать расходы и скинуть ссылки каждому

            // TODO Добавить ограничение завершения только на лидера группы

            // TODO рефакторинг

            case "Создать команду":
                // TODO fix it
                // dbDriver.AddUser(message.Chat.Username!, chatId);
                var guid = new Guid();
                log.LogInformation("{username} created team {guid} in {chatId}",
                    message.Chat.Username, guid, chatId);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Id комманды: {guid}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;

            case "Присоединиться к команде":
                // TODO fix it
                // dbDriver.AddUser(message.Chat.Username!, chatId);
                // Скинь гуид
                // log.LogInformation("{username} joined team {guid} in {chatId}",
                //     message.Chat.Username, guid, chatId);
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Отправь Id команды",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
        }
    }
}