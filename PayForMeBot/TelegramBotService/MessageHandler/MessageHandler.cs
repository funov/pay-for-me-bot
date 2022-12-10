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
    private static HashSet<string> openTeamFlags = new() {"/start", "Начать"};
    private static string[] teamSelectionLabels = {"Создать команду", "Присоединиться к команде"};

    private static HashSet<string> closeTeamFlags = new() {"/end", "Завершить"};
    private static string[] closeTeamLabels = {"Подсчитать расходы и прислать реквизиты"};

    private static HashSet<string> helpFlags = new() {"/help", "help", "Помощь"};

    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    public MessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IReceiptApiClient receiptApiClient,
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
                text: GetHelpMessage(),
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
                text: GetHelpMessage(),
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


        // if (closeTeamFlags.Contains(message.Text!) && message.Chat.Id != teamLeadId)
        // {
        //     await client.SendTextMessageAsync(
        //         chatId: chatId,
        //         text: "Только лидер команды может завершить работу",
        //         cancellationToken: cancellationToken
        //     );
        // }
        //
        // if (closeTeamFlags.Contains(message.Text!) && message.Chat.Id == teamLeadId)
        // {
        //     await client.SendTextMessageAsync(
        //         chatId: chatId,
        //         text: "Test closing team",
        //         replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(closeTeamLabels),
        //         cancellationToken: cancellationToken);
        //     return;
        // }


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

    public async Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        if (message.Photo == null)
            return;

        var chatId = message.Chat.Id;

        var fileId = message.Photo.Last().FileId;
        var fileInfo = await client.GetFileAsync(fileId, cancellationToken: cancellationToken);

        if (fileInfo.FilePath == null)
            throw new ArgumentException("FilePath is null");

        var filePath = fileInfo.FilePath;

        log.LogInformation("Received a '{photoPath}' message in chat {chatId}", filePath, chatId);

        var encryptedContent = Array.Empty<byte>();

        if (fileInfo.FileSize != null)
        {
            using var stream = new MemoryStream((int) fileInfo.FileSize.Value);
            await client.DownloadFileAsync(filePath, stream, cancellationToken);
            encryptedContent = stream.ToArray();
        }

        await ShowProductSelectionButtons(client, chatId, encryptedContent, cancellationToken, message);
    }

    private async Task ShowProductSelectionButtons(ITelegramBotClient client, long chatId, byte[] encryptedContent,
        CancellationToken cancellationToken, Message message)
    {
        var problemText = "Не удалось обработать чек, попробуйте снова";

        try
        {
            log.LogInformation("Send request to receipt api in {chatId}", chatId);

            var receipt = await receiptApiClient.GetReceipt(encryptedContent);
            var products = receipt.Products;

            if (products != null)
            {
                await SendProductsMessages(client, chatId, products, message.Chat.Username, cancellationToken);
                return;
            }
        }
        catch (ReceiptNotFoundException)
        {
            problemText = "Не удалось обработать чек, возможно на фото нет чека";
        }
        catch (JsonException)
        {
            problemText = "Обработка изображений временно недоступна";
        }

        log.LogInformation("Received a '{problemText}' message in chat {chatId}", problemText, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: problemText,
            cancellationToken: cancellationToken);
    }

    private async Task SendProductsMessages(ITelegramBotClient client, long chatId, IEnumerable<Product> products,
        string telegramUserName, CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            var text = $"{product.Name}";
            var guid = Guid.NewGuid();
            var guidReceipt = Guid.NewGuid();

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                guid,
                $"{product.TotalPrice} р.",
                $"{product.Count} шт.",
                "🛒");

            // TODO fix it
            // dbDriver.AddProduct(guid, product, guidReceipt, telegramUserName, message.Chat.Id);

            log.LogInformation("Send product {ProductId} inline button to chat {ChatId}", guid, chatId);

            await client.SendTextMessageAsync(
                chatId,
                text,
                replyMarkup: inlineKeyboardMarkup,
                disableNotification: true,
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback,
        CancellationToken cancellationToken)
    {
        if (callback.Message != null && callback.Data != null && Guid.TryParse(callback.Data, out var guid))
        {
            if (callback.Message.ReplyMarkup == null)
                throw new InvalidOperationException();

            var inlineKeyboard = callback.Message.ReplyMarkup.InlineKeyboard.First().ToArray();

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                guid,
                inlineKeyboard[0].Text,
                inlineKeyboard[1].Text,
                inlineKeyboard[2].Text == "🛒" ? "✅" : "🛒");

            if (inlineKeyboard[2].Text == "🛒")
            {
                log.LogInformation("User {UserId} decided to pay for the product {ProductId}", callback.From, guid);

                // TODO fix it
                // var teamId = dbDriver.GetTeamIdByUserTelegramId(callback.From.Username!);
                // dbDriver.AddUserProductBinding(callback.From.Username, teamId, guid);
            }
            else
            {
                log.LogInformation("User {UserId} refused to pay for the product {ProductId}", callback.From, guid);

                // TODO fix it
                // var teamId = dbDriver.GetTeamIdByUserTelegramId(callback.From.Username!);
                // dbDriver.DeleteUserProductBinding(callback.From.Username, teamId, guid);
            }

            await client.EditMessageTextAsync(
                callback.Message.Chat.Id,
                callback.Message.MessageId,
                callback.Message.Text ?? throw new InvalidOperationException(),
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        else
            await client.AnswerCallbackQueryAsync(callback.Id, cancellationToken: cancellationToken);
    }
}