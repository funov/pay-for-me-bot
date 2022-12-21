using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqliteProvider.Repositories.UserRepository;
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
    private readonly ITeamAdditionStageMessageHandler teamAdditionStageMessageHandler;
    private readonly IProductsSelectionStageMessageHandler productsSelectionStageMessageHandler;
    private readonly IPaymentStageMessageHandler paymentStageMessageHandler;
    private readonly IUserRepository userRepository;

    public TelegramBotService(
        ILogger<TelegramBotService> log,
        IConfiguration config,
        ITeamAdditionStageMessageHandler teamAdditionStageMessageHandler,
        IProductsSelectionStageMessageHandler productsSelectionStageMessageHandler,
        IPaymentStageMessageHandler paymentStageMessageHandler,
        IUserRepository userRepository)
    {
        this.log = log;
        this.config = config;
        this.teamAdditionStageMessageHandler = teamAdditionStageMessageHandler;
        this.productsSelectionStageMessageHandler = productsSelectionStageMessageHandler;
        this.paymentStageMessageHandler = paymentStageMessageHandler;
        this.userRepository = userRepository;
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

                var user = userRepository.GetUser(chatId);
                currentStage = user.Stage!;
            }
            else if (update.CallbackQuery != null)
            {
                chatId = update.CallbackQuery!.From.Id;
                
                var user = userRepository.GetUser(chatId);
                currentStage = user.Stage!;
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
                        await teamAdditionStageMessageHandler.HandleTextAsync(client, update.Message,
                            cancellationToken);
                        break;
                    case "middle":
                        await productsSelectionStageMessageHandler.HandleTextAsync(client, update.Message,
                            cancellationToken);
                        break;
                    case "end":
                        await paymentStageMessageHandler.HandleTextAsync(client, update.Message, cancellationToken);
                        break;
                }

                break;

            case { Type: MessageType.Photo }:
                if (currentStage == "middle")
                    await productsSelectionStageMessageHandler.HandlePhotoAsync(client, update.Message,
                        cancellationToken);
                break;
        }

        switch (update)
        {
            case { Type: UpdateType.CallbackQuery }:
                if (update.CallbackQuery != null && currentStage == "middle")
                    await productsSelectionStageMessageHandler.HandleCallbackQuery(client, update.CallbackQuery,
                        cancellationToken);
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