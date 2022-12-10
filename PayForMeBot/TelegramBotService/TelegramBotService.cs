using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.TelegramBotService.Exceptions;
using PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.MiddleStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;

namespace PayForMeBot.TelegramBotService;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IConfiguration config;
    private readonly IStartStageMessageHandler startHandler;
    private readonly IMiddleStageMessageHandler middleHandler;
    private readonly IEndStageMessageHandler endHandler;
    private readonly IDbDriver dbDriver;

    public TelegramBotService(ILogger<ReceiptApiClient.ReceiptApiClient> log, IConfiguration config,
        IStartStageMessageHandler startHandler, IMiddleStageMessageHandler middleHandler,
        IEndStageMessageHandler endHandler, IDbDriver dbDriver)
    {
        this.log = log;
        this.config = config;
        this.startHandler = startHandler;
        this.middleHandler = middleHandler;
        this.endHandler = endHandler;
        this.dbDriver = dbDriver;
    }

    public async Task Run()
    {
        var token = config.GetValue<string>("TELEGRAM_BOT_TOKEN");

        if (token == null)
            throw new NullTokenException("Configuration error");

        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
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
        var chatId = update.Message!.Chat.Id;
        var teamId = dbDriver.GetTeamIdByUserChatId(chatId);
        var currentStage = dbDriver.GetUserStage(chatId, teamId)!;

        // TODO refactoring :(

        switch (currentStage)
        {
            case "start":
                switch (update.Message)
                {
                    case { Type: MessageType.Text }:
                        await startHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                }

                break;
            case "middle":
                switch (update.Message)
                {
                    case { Type: MessageType.Text }:
                        await middleHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;

                    case { Type: MessageType.Photo }:
                        await middleHandler.HandlePhotoAsync(client, update.Message, cancellationToken);
                        break;
                }

                switch (update)
                {
                    case { Type: UpdateType.CallbackQuery }:
                        if (update.CallbackQuery != null)
                            await middleHandler.HandleCallbackQuery(client, update.CallbackQuery, cancellationToken);
                        break;
                }

                break;
            case "end":
                switch (update.Message)
                {
                    case { Type: MessageType.Text }:
                        await endHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                }

                break;
        }
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

        return Run();
    }
}