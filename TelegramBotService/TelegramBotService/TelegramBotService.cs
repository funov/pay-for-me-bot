using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.Exceptions;
using TelegramBotService.MessageHandlers.PaymentStageMessageHandler;
using TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;
using TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

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
        if (update.Type == UpdateType.CallbackQuery)
        {
            var callback = update.CallbackQuery;
            var chatId = callback!.From.Id;
            var user = userRepository.GetUser(chatId);
            var currentStage = GetCurrentStage(user);

            if (update.CallbackQuery != null && currentStage == UserStage.ProductSelection)
                await productsSelectionStageMessageHandler.HandleCallbackQuery(client, callback, cancellationToken);
        }

        if (update.Message is { Type: MessageType.Text })
        {
            var message = update.Message;
            var chatId = message.Chat.Id;
            var user = userRepository.GetUser(chatId);
            var currentStage = GetCurrentStage(user);

            switch (currentStage)
            {
                case UserStage.TeamAddition:
                    await teamAdditionStageMessageHandler.HandleTextAsync(client, message, cancellationToken);
                    break;
                case UserStage.ProductSelection:
                    await productsSelectionStageMessageHandler.HandleTextAsync(client, message, cancellationToken);
                    break;
                case UserStage.Payment:
                    await paymentStageMessageHandler.HandleTextAsync(client, message, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Incorrect user stage {currentStage}");
            }
        }

        if (update.Message is { Type: MessageType.Photo })
        {
            var chatId = update.Message.Chat.Id;
            var user = userRepository.GetUser(chatId);
            var currentStage = GetCurrentStage(user);

            if (currentStage == UserStage.ProductSelection)
                await productsSelectionStageMessageHandler.HandlePhotoAsync(client, update.Message, cancellationToken);
        }
    }

    private static UserStage GetCurrentStage(SqliteProvider.Models.User? user)
        => user?.Stage ?? UserStage.TeamAddition;

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