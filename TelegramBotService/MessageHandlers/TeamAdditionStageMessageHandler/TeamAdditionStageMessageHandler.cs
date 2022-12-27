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

        teamSelectionLabels = new[] {botPhrasesProvider.CreateTeamButton!, botPhrasesProvider.JoinTeamButton!};
        goToSplitPurchasesButtons = new[] {botPhrasesProvider.GoToSplitPurchases!};
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

            var teamUserChatIds = userRepository
                .GetUserChatIdsByTeamId(teamId);
            var teamUsersUsernames = teamUserChatIds
                .Select(x => userRepository.GetUser(x)!.Username);

            await SendTeamListAsync(client, chatId, cancellationToken, teamUsersUsernames!);

            userRepository.AddUser(userName, chatId, teamId);
            userRepository.ChangeUserStage(chatId, teamId, UserStage.ProductSelection);

            await SendNewTeammateUsernameToTeammatesAsync(teamUserChatIds, userName, client, cancellationToken);

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

    private async Task SendNewTeammateUsernameAsync(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken, string newUsername)
    {
        var text = $"@{newUsername} присоединился к команде!";
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task SendTeamListAsync(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken, IEnumerable<string> teamUsersUsernames)
    {
        var header = "Добро пожаловать в команду!\n\nС тобой в команде:\n\n";
        var body = string.Join("\n\n", teamUsersUsernames.Select(x => $"@{x}"));
        var text = header + body;
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task SendNewTeammateUsernameToTeammatesAsync(
        IEnumerable<long> teamUserChatIds,
        string? userName,
        ITelegramBotClient client,
        CancellationToken cancellationToken)
    {
        foreach (var teamUserChatId in from teamUserChatId in teamUserChatIds
                 let user = userRepository.GetUser(teamUserChatId)
                 let teamUserName = user!.Username!
                 where userName != null && teamUserName != userName
                 select teamUserChatId)
        {
            await SendNewTeammateUsernameAsync(client, teamUserChatId, cancellationToken, userName!);
        }
    }
}