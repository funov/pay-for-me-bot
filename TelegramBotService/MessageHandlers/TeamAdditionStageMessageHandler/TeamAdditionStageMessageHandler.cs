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
using TelegramBotService.ButtonUtils.KeyboardMarkup;
using TelegramBotService.ButtonUtils.ProductInlineButtonSender;
using TelegramBotService.Models;

namespace TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

public class TeamAdditionStageMessageHandler : ITeamAdditionStageMessageHandler
{
    private readonly string[] teamSelectionLabels;
    private readonly string[] splitPurchasesButtons;

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
        splitPurchasesButtons = new[] { botPhrasesProvider.GoToSplitPurchases! };
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (message.Text! == botPhrasesProvider.CreateTeamButton)
        {
            await HandleCreateTeamAsync(client, chatId, userName, cancellationToken);
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
            return;
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
            await HandleTeamIdAsync(client, chatId, userName, teamId, cancellationToken);
        }
    }

    private async Task HandleCreateTeamAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        CancellationToken cancellationToken)
    {
        var userTeamId = Guid.NewGuid();

        log.LogInformation("@{username} created a team {guid} in chat {chatId}",
            userName, userTeamId, chatId);

        userRepository.AddUser(userName, chatId, userTeamId);

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
            replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(splitPurchasesButtons),
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleTeamIdAsync(
        ITelegramBotClient client,
        long chatId,
        string username,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        log.LogInformation("@{username} joined team {guid} in {chatId}",
            username, teamId, chatId);

        var teamChatIds = userRepository
            .GetUserChatIdsByTeamId(teamId)
            .ToArray();
        var teamUsernames = teamChatIds
            .Select(userChatId => userRepository.GetUser(userChatId)!.Username);

        await SendTeamListAsync(client, chatId, teamUsernames!, cancellationToken);

        log.LogInformation("Send team list to @{username} with team {guid} in chat {chatId}",
            username, teamId, chatId);

        userRepository.AddUser(username, chatId, teamId);
        userRepository.ChangeUserStage(chatId, teamId, UserStage.ProductSelection);

        await SendNewUserToTeammatesAsync(client, teamChatIds, username, teamId, cancellationToken);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: botPhrasesProvider.StartAddingProducts!,
            parseMode: ParseMode.Html,
            replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(splitPurchasesButtons),
            cancellationToken: cancellationToken
        );

        var products = productRepository.GetProductsByTeamId(teamId).ToArray();
        await productInlineButtonSender.SendProductsInlineButtonsAsync(
            client,
            chatId,
            username,
            products.Select(product => mapper.Map<Product>(product)),
            products.Select(product => product.Id),
            cancellationToken);
    }

    private static async Task SendTeamListAsync(
        ITelegramBotClient client,
        long chatId,
        IEnumerable<string> teamUsernames,
        CancellationToken cancellationToken)
    {
        var body = string.Join("\n\n", teamUsernames.Select(usernames => $"@{usernames}"));

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: $"Добро пожаловать в команду!\n\nС тобой в команде:\n\n{body}",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task SendNewUserToTeammatesAsync(
        ITelegramBotClient client,
        IEnumerable<long> teamChatIds,
        string? username,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        foreach (var chatId in from teamChatId in teamChatIds
                 let user = userRepository.GetUser(teamChatId)
                 let teamUserName = user!.Username!
                 where username != null && teamUserName != username
                 select teamChatId)
        {
            log.LogInformation("@{username} created a team {guid} in chat {chatId}",
                username, teamId, chatId);

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"@{username} присоединился к команде!",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }
}