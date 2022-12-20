using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayForMeBot.TelegramBotService.Exceptions;
using PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.MiddleStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;
using SqliteProvider.SqliteProvider;

namespace PayForMeBot.TelegramBotService;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger<TelegramBotService> log;
    private readonly IConfiguration config;
    private readonly IStartStageMessageHandler startHandler;
    private readonly IMiddleStageMessageHandler middleHandler;
    private readonly IEndStageMessageHandler endHandler;
    private readonly ISqliteProvider sqliteProvider;

    public TelegramBotService(ILogger<TelegramBotService> log, IConfiguration config,
        IStartStageMessageHandler startHandler, IMiddleStageMessageHandler middleHandler,
        IEndStageMessageHandler endHandler, ISqliteProvider sqliteProvider)
    {
        this.log = log;
        this.config = config;
        this.startHandler = startHandler;
        this.middleHandler = middleHandler;
        this.endHandler = endHandler;
        this.sqliteProvider = sqliteProvider;
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
        // TODO refactoring :(

        string currentStage;
        long chatId;

        try
        {
            if (update.Message != null)
            {
                chatId = update.Message!.Chat.Id;
                var teamId = sqliteProvider.GetTeamIdByUserChatId(chatId);
                currentStage = sqliteProvider.GetUserStage(chatId, teamId)!;
            }
            else if (update.CallbackQuery != null)
            {
                chatId = update.CallbackQuery!.From.Id;
                var teamId = sqliteProvider.GetTeamIdByUserChatId(chatId);
                currentStage = sqliteProvider.GetUserStage(chatId, teamId)!;
            }
            else
            {
                currentStage = "start";
            }
        }
        catch (NullReferenceException)
        {
            currentStage = "start";
        }

        switch (update.Message)
        {
            case { Type: MessageType.Text }:
                switch (currentStage)
                {
                    case "start":
                        await startHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                    case "middle":
                        await middleHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                    case "end":
                        await endHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                }

                break;

            case { Type: MessageType.Photo }:
                if (currentStage == "middle")
                    await middleHandler.HandlePhotoAsync(client, update.Message, cancellationToken);
                break;
        }

        switch (update)
        {
            case { Type: UpdateType.CallbackQuery }:
                if (update.CallbackQuery != null && currentStage == "middle")
                {
                    log.LogInformation("{userName}", currentStage);
                    await middleHandler.HandleCallbackQuery(client, update.CallbackQuery, cancellationToken);
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