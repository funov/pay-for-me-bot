using AutoMapper;
using Microsoft.Extensions.Logging;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Types;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.Models;
using TelegramBotService.ProductInlineButtonSender;

namespace TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

public class TeamAdditionStageMessageHandler : ITeamAdditionStageMessageHandler
{
    private readonly string[] teamSelectionLabels;
    private readonly string[] goToSplitPurchasesButtons;

    private readonly ILogger<TeamAdditionStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IProductInlineButtonSender productInlineButtonSender;
    private readonly IMapper mapper;

    public TeamAdditionStageMessageHandler(
        ILogger<TeamAdditionStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IBotPhrasesProvider botPhrasesProvider,
        IProductInlineButtonSender productInlineButtonSender,
        IMapper mapper)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.botPhrasesProvider = botPhrasesProvider;
        this.productInlineButtonSender = productInlineButtonSender;
        this.mapper = mapper;

        teamSelectionLabels = new[] { botPhrasesProvider.CreateTeamButton!, botPhrasesProvider.JoinTeamButton! };
        goToSplitPurchasesButtons = new[] { botPhrasesProvider.GoToSplitPurchases! };
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (message.Text! == botPhrasesProvider.CreateTeamButton)
        {
            var userTeamId = Guid.NewGuid();

            log.LogInformation("@{username} created a team {guid} in chat {chatId}",
                userName, userTeamId, chatId);

            userRepository.AddUser(message.Chat.Username!, chatId, userTeamId);

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"Код вашей комманды:\n\n<code>{userTeamId}</code>",
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            userRepository.ChangeUserStage(chatId, userTeamId, UserStage.ProductSelection);

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.StartAddingProducts!,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                cancellationToken: cancellationToken
            );
            return;
        }

        if (message.Text! == botPhrasesProvider.JoinTeamButton)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.SendMeTeamId!,
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );
        }

        switch (message.Text!)
        {
            case "/start":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.CreateOrJoinTeam!,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                    cancellationToken: cancellationToken);
                break;
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.Help!,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;
        }

        if (Guid.TryParse(message.Text, out var teamId))
        {
            log.LogInformation("@{username} joined team {guid} in {chatId}",
                userName, teamId, chatId);

            userRepository.AddUser(userName, chatId, teamId);
            userRepository.ChangeUserStage(chatId, teamId, UserStage.ProductSelection);

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.StartAddingProducts!,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                cancellationToken: cancellationToken
            );

            var products = productRepository.GetProductsByTeamId(teamId).ToArray();
            await productInlineButtonSender.SendProductsInlineButtonsAsync(
                client,
                chatId,
                userName,
                products.Select(product => mapper.Map<Product>(product)),
                products.Select(product => product.Id),
                cancellationToken);
        }
    }
}