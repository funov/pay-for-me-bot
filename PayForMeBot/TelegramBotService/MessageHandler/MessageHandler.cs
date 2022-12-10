using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.DbDriver;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot.Types.ReplyMarkups;

namespace PayForMeBot.TelegramBotService.MessageHandler;

public class MessageHandler : IMessageHandler
{
    private static HashSet<string> openTeamFlags = new() { "/start", "start", "Начать" };
    private static string[] teamSelectionLabels = { "Создать команду", "Присоединиться к команде" };

    private static HashSet<string> closeTeamFlags = new() { "/end", "end", "Завершить" };
    private static string[] closeTeamLabels = { "Подсчитать расходы и прислать реквизиты" };

    private static HashSet<string> helpFlags = new() { "/help", "help", "Помощь" };

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

    public MessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IReceiptApiClient receiptApiClient,
        IKeyboardMarkup keyboardMarkup, IDbDriver dbDriver)
    {
        this.log = log;
        this.receiptApiClient = receiptApiClient;
        this.keyboardMarkup = keyboardMarkup;
        this.dbDriver = dbDriver;
    }

    private static Product ParseTextToProduct(string text)
    {
        var productName = text.Split(" ").Take(text.Length - 1).ToString();
        if (double.TryParse(text.Split(" ").Last(), out var price))
            return new Product
            {
                Count = 1,
                Name = productName,
                Price = price,
                TotalPrice = price
            };

        throw new ArgumentException("Неправильная цена / нарушен формат строки");
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

        if (closeTeamFlags.Contains(message.Text!))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Test closing team / Cкинь свои реквизиты",
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(closeTeamLabels),
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

        switch (message.Text!)
        {
            // TODO Брать их из массива (teamSelectionLabels)

            // TODO Подсчитать расходы и скинуть ссылки каждому

            // TODO Добавить ограничение завершения только на лидера группы

            // TODO рефакторинг

            case "Создать команду":
                // TODO fix it
                // dbDriver.AddUser(message.Chat.Username!, chatId);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Создана",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );

                break;
            case "Присоединиться к команде":
                // TODO fix it
                // dbDriver.AddUser(message.Chat.Username!, chatId);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Присоединяюсь!",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;

            case "Подсчитать расходы и прислать реквизиты":
                // dbDriver.AddUser(message.Chat.Username!, chatId);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Скинь мне реквизиты",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
        }

        // TODO Если прислал свои реквизиты, получает реквизиты остальных

        if (!true)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Держи реквизиты тех, кому ты должен",
                cancellationToken: cancellationToken
            );
        }
    }

    public Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}