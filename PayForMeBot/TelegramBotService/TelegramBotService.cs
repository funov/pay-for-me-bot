using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.ReceiptApiClient.JsonObjects;

namespace PayForMeBot.TelegramBotService;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IConfiguration config;
    private readonly IReceiptApiClient receiptApiClient;

    public TelegramBotService(
        ILogger<ReceiptApiClient.ReceiptApiClient> log, 
        IConfiguration config, 
        IReceiptApiClient receiptApiClient)
    {
        this.log = log;
        this.config = config;
        this.receiptApiClient = receiptApiClient;
    }

    public async Task Run()
    {
        var token = config.GetValue<string>("TELEGRAM_BOT_TOKEN");
        
        if (token == null)
        {
            throw new ArgumentException("Token doesn't exists in appsettings.json");
        }
        
        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] {UpdateType.Message, UpdateType.CallbackQuery},
            ThrowPendingUpdates = true
        };
        
        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        
        log.LogInformation("Start listening for @{userName}", me.Username);

        Console.ReadLine();
        
        cts.Cancel();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        switch (update.Message)
        {
            case { Type: MessageType.Text }:
                await HandleTextAsync(client, update.Message, cancellationToken);
                break;
            
            case { Type: MessageType.Photo }:
                await HandlePhotoAsync(client, update.Message, cancellationToken);
                break;
        }

        switch (update)
        {
            case { Type: UpdateType.CallbackQuery }:
                if (update.CallbackQuery != null)
                    await HandleCallbackQuery(client, update.CallbackQuery, cancellationToken);
                break;
        }
    }
    
    private async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        
        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: $"Что такое {message.Text}?",
            cancellationToken: cancellationToken);
    }

    private async Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
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
            using var stream = new MemoryStream((int)fileInfo.FileSize.Value);
            await client.DownloadFileAsync(filePath, stream, cancellationToken);
            encryptedContent = stream.ToArray();
        }
        
        await ShowProductSelectionButtons(client, chatId, encryptedContent, cancellationToken);
    }
    
    private async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (callback?.Data != null && Guid.TryParse(callback.Data, out var guid))
        {
            var inlineKeyboard = callback.Message!.ReplyMarkup!.InlineKeyboard.First().ToArray();

            var inlineKeyboardMarkup = GetInlineKeyboardMarkup(
                guid, 
                inlineKeyboard[0].Text,
                inlineKeyboard[1].Text,
                inlineKeyboard[2].Text == "🛒" ? "✅" : "🛒");

            await client.EditMessageTextAsync(
                callback.Message!.Chat.Id,
                callback.Message.MessageId,
                callback.Message!.Text,
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        else
            await client.AnswerCallbackQueryAsync(callback.Id, cancellationToken: cancellationToken);
    }

    private async Task ShowProductSelectionButtons(ITelegramBotClient client, long chatId, byte[] encryptedContent, 
        CancellationToken cancellationToken)
    {
        string problemText;
        
        try
        {
            var receiptApiResponse = await receiptApiClient.GetReceiptApiResponse(encryptedContent);
            var products = receiptApiResponse.Data?.Json?.Items;

            if (products != null)
                await SendProductsButtons(client, chatId, products, cancellationToken);
            return;
        }
        catch (ReceiptNotFoundException)
        {
            problemText = "Не удалось обработать чек, возможно на фото нет чека";
        }
        catch (JsonException)
        {
            problemText = "Обработка изображений временно недоступна";
        }
        
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: problemText,
            cancellationToken: cancellationToken);
    }

    private async Task SendProductsButtons(ITelegramBotClient client, long chatId, IEnumerable<Item> products, 
        CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            var text = $"{product.Name}";
            var guid = Guid.NewGuid();
                
            var inlineKeyboardMarkup = GetInlineKeyboardMarkup(
                guid, 
                $"{GetRublesPrice(product.TotalPrice)} р.",
                $"{product.Count} шт.",
                "🛒");

            await client.SendTextMessageAsync(
                chatId, 
                text, 
                replyMarkup: inlineKeyboardMarkup, 
                disableNotification: true,
                cancellationToken: cancellationToken);
        }
    }

    private static InlineKeyboardMarkup GetInlineKeyboardMarkup(Guid guid, string priceText, string countText, 
        string buyButtonText)
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(priceText),
                InlineKeyboardButton.WithCallbackData(countText),
                InlineKeyboardButton.WithCallbackData(buyButtonText, guid.ToString()),
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }

    private static double GetRublesPrice(int kopecksPrice)
    {
        var kopecks = kopecksPrice % 100;
        var rubles = kopecksPrice / 100;

        return rubles + kopecks / 100.0;
    }
    
    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, 
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };
        
        log.LogError(errorMessage);
        return Task.CompletedTask;
    }
}