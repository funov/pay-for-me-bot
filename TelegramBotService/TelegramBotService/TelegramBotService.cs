using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqliteProvider.SqliteProvider;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.Exceptions;
using TelegramBotService.TelegramBotService.MessageHandlers.PaymentStageMessageHandler;
using TelegramBotService.TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;
using TelegramBotService.TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

namespace TelegramBotService.TelegramBotService;

public class TelegramBotService : ITelegramBotService
{
    private readonly ILogger<TelegramBotService> log;
    private readonly IConfiguration config;
    private readonly ITeamAdditionStageMessageHandler _teamAdditionHandler;
    private readonly IProductsSelectionStageMessageHandler _productsSelectionHandler;
    private readonly IPaymentStageMessageHandler _paymentHandler;
    private readonly ISqliteProvider sqliteProvider;

    public TelegramBotService(ILogger<TelegramBotService> log, IConfiguration config,
        ITeamAdditionStageMessageHandler teamAdditionHandler, IProductsSelectionStageMessageHandler productsSelectionHandler,
        IPaymentStageMessageHandler paymentHandler, ISqliteProvider sqliteProvider)
    {
        this.log = log;
        this.config = config;
        this._teamAdditionHandler = teamAdditionHandler;
        this._productsSelectionHandler = productsSelectionHandler;
        this._paymentHandler = paymentHandler;
        this.sqliteProvider = sqliteProvider;
    }

    public async Task Run()
    {
        var token = config.GetValue<string>("TELEGRAM_BOT_TOKEN");

        if (token == null)
            throw new NullTelegramTokenException("Configuration error");

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
                        await _teamAdditionHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                    case "middle":
                        await _productsSelectionHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                    case "end":
                        await _paymentHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                }

                break;

            case { Type: MessageType.Photo }:
                if (currentStage == "middle")
                    await _productsSelectionHandler.HandlePhotoAsync(client, update.Message, cancellationToken);
                break;
        }

        switch (update)
        {
            case { Type: UpdateType.CallbackQuery }:
                if (update.CallbackQuery != null && currentStage == "middle")
                {
                    log.LogInformation("{userName}", currentStage);
                    await _productsSelectionHandler.HandleCallbackQuery(client, update.CallbackQuery, cancellationToken);
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