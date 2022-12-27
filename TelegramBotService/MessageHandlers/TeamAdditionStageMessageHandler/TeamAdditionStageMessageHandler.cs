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

    public TeamAdditionStageMessageHandler(
        ILogger<TeamAdditionStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IBotPhrasesProvider botPhrasesProvider)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.botPhrasesProvider = botPhrasesProvider;

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
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                cancellationToken: cancellationToken
            );

            var pastProducts = productRepository.GetProductsByTeamId(teamId);

            foreach (var pastProduct in pastProducts)
            {
                var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                    pastProduct.Id,
                    $"{pastProduct.TotalPrice} р.",
                    $"{pastProduct.Count} шт.",
                    "🛒");

                log.LogInformation("Send product {ProductId} inline button to @{username} in chat {ChatId}",
                    userName, pastProduct.Id, chatId);

                await client.SendTextMessageAsync(
                    chatId,
                    pastProduct.Name!,
                    replyMarkup: inlineKeyboardMarkup,
                    disableNotification: true,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private bool IsUserInTeam(long userChatId) => userRepository.IsUserInDb(userChatId);
}