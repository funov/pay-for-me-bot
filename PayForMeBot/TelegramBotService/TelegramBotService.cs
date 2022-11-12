using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using PayForMeBot.ReceiptApiClient;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PayForMeBot.TelegramBotService;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IConfiguration config;
    private readonly IReceiptApiClient receiptApiClient;

    public TelegramBotService(ILogger<ReceiptApiClient.ReceiptApiClient> log, IConfiguration config, IReceiptApiClient receiptApiClient)
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
            AllowedUpdates = Array.Empty<UpdateType>()
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
        if (update.Message is { Type: MessageType.Photo })
        {
            if (update.Message.Photo == null)
                return;

            var fileId = update.Message.Photo.Last().FileId;
            var fileInfo = await client.GetFileAsync(fileId, cancellationToken: cancellationToken);

            if (fileInfo.FilePath == null)
                throw new ArgumentException("FilePath is null");
            
            var filePath = fileInfo.FilePath;
            
            log.LogInformation("Received a '{photoPath}' message in chat {chatId}", filePath, update.Message.Chat.Id);
            
            var encryptedContent = Array.Empty<byte>();

            if (fileInfo.FileSize != null)
            {
                using var stream = new MemoryStream((int)fileInfo.FileSize.Value);
                await client.DownloadFileAsync(filePath, stream, cancellationToken);
                encryptedContent = stream.ToArray();
            }

            var receiptApiResponse = await receiptApiClient.SendReceiptImage(encryptedContent);

            var botMessageText =
                $"Название магазина: {receiptApiResponse.Data.Json.ShopName}\n" +
                $"Суммарная стоимость: {receiptApiResponse.Data.Json.TotalSum / 100.0} рублей\n\n" +
                "Товары:\n" +
                $"{Strings.Join(receiptApiResponse.Data.Json.Items.Select(x => $"{x.Name} — {x.TotalPrice / 100.0} рублей").ToArray(), "\n")}";

            await client.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: botMessageText,
                cancellationToken: cancellationToken);

            return;
        }

        if (update.Message is not { Text: { } messageText } message)
            return;
        
        var chatId = message.Chat.Id;
        
        log.LogInformation("Received a '{messageText}' message in chat {chatId}", messageText, chatId);
        
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: "You said:\n" + messageText,
            cancellationToken: cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, 
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
        
        log.LogError(errorMessage);
        return Task.CompletedTask;
    }
}