using System.Text;
using System.Text.RegularExpressions;
using DebtsCalculator;
using Microsoft.Extensions.Logging;
using SqliteProvider.Types;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.ButtonUtils.KeyboardMarkup;

namespace TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public class PaymentStageMessageHandler : IPaymentStageMessageHandler
{
    private static string telephoneNumberRegexPattern = @"^((7|8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
    private static string tinkoffLinkRegexPattern = @"https://www.tinkoff.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+";

    private readonly ILogger<PaymentStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IDebtsCalculator debtsCalculator;

    private readonly string?[] teamSelectionLabels;
    private readonly char[] requisitesSeparators;

    public PaymentStageMessageHandler(
        ILogger<PaymentStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository,
        IBotPhrasesProvider botPhrasesProvider,
        IDebtsCalculator debtsCalculator)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;
        this.botPhrasesProvider = botPhrasesProvider;
        this.debtsCalculator = debtsCalculator;

        teamSelectionLabels = new[] { botPhrasesProvider.CreateTeamButton, botPhrasesProvider.JoinTeamButton };
        requisitesSeparators = new[] { '\n', ' ' };
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (!userRepository.IsUserSentRequisite(chatId))
        {
            await HandleAddRequisitesAsync(client, chatId, userName, message.Text!, cancellationToken);
        }

        if (message.Text! == "/help")
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.Help!,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAddRequisitesAsync(
        ITelegramBotClient client,
        long chatId,
        string userName,
        string messageText,
        CancellationToken cancellationToken)
    {
        var user = userRepository.GetUser(chatId);
        var teamId = user!.TeamId;

        if (IsRequisiteValid(messageText))
        {
            log.LogInformation("Requisite is valid '{messageText}' in chat {chatId} from @{userName}",
                messageText, chatId, userName);

            AddPhoneNumberAndTinkoffLink(messageText, chatId);

            if (userRepository.IsAllTeamHasPhoneNumber(teamId))
            {
                await FinishTeamAsync(client, chatId, teamId, cancellationToken);
                return;
            }

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.WaitingOtherUsersRequisites!,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        log.LogInformation("Requisite isn't valid '{messageText}' in chat {chatId} from @{userName}",
            messageText, chatId, userName);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: botPhrasesProvider.RequisitesSendingError!,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task FinishTeamAsync(
        ITelegramBotClient client,
        long chatId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var userIdToBuyerIdToDebt = debtsCalculator.GetUserIdToBuyerIdToDebt(teamId);
        var teamChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

        foreach (var teamChatId in teamChatIds)
        {
            await SendRequisitesAndDebtsAsync(client, teamChatId, userIdToBuyerIdToDebt[teamChatId], cancellationToken);

            userRepository.ChangeUserStage(chatId, teamId, UserStage.TeamAddition);

            await client.SendTextMessageAsync(
                chatId: teamChatId,
                text: botPhrasesProvider.CreateOrJoinTeam!,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels!),
                cancellationToken: cancellationToken);
        }

        // TODO
        // constraints (foreign key) 
        // транзакции BeginTransaction, по очереди все дропает
        userRepository.DeleteAllUsersByTeamId(teamId);
        productRepository.DeleteAllProductsByTeamId(teamId);
        userProductBindingRepository.DeleteAllUserProductBindingsByTeamId(teamId);
    }

    private void AddPhoneNumberAndTinkoffLink(string messageText, long chatId)
    {
        messageText = messageText.Trim();
        var requisites = messageText.Split(requisitesSeparators, StringSplitOptions.RemoveEmptyEntries);

        switch (requisites.Length)
        {
            case 1:
                userRepository.AddPhoneNumber(chatId, requisites[0]);
                break;
            case 2:
            {
                if (IsTelephoneNumberValid(requisites[0]))
                    userRepository.AddPhoneNumber(chatId, requisites[0]);
                else if (IsTelephoneNumberValid(requisites[1]))
                    userRepository.AddPhoneNumber(chatId, requisites[1]);

                if (IsTinkoffLinkValid(requisites[0]))
                    userRepository.AddTinkoffLink(chatId, requisites[0]);
                else if (IsTinkoffLinkValid(requisites[1]))
                    userRepository.AddTinkoffLink(chatId, requisites[1]);

                break;
            }
        }
    }

    private async Task SendRequisitesAndDebtsAsync(ITelegramBotClient client, long chatId,
        Dictionary<long, double> buyersToMoney, CancellationToken cancellationToken)
    {
        var message = GetDebtMessageText(buyersToMoney);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        if (message != botPhrasesProvider.WithoutDebt)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.Goodbye!,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }

    private string GetDebtMessageText(Dictionary<long, double> buyersToMoney)
    {
        var debtMessage = new StringBuilder();

        if (buyersToMoney.Count == 0)
            return botPhrasesProvider.WithoutDebt!;

        foreach (var buyerChatId in buyersToMoney.Keys)
        {
            var buyer = userRepository.GetUser(buyerChatId)!;
            var typeRequisites = userRepository.GetRequisiteType(buyerChatId);

            switch (typeRequisites)
            {
                case RequisiteType.PhoneNumber:
                    debtMessage.Append(GetRequisitesAndDebtsMessageText(buyer.Username!, buyer.PhoneNumber!,
                        buyersToMoney[buyerChatId]));
                    break;
                case RequisiteType.PhoneNumberAndTinkoffLink:
                    debtMessage.Append(GetRequisitesAndDebtsMessageText(buyer.Username!, buyer.PhoneNumber!,
                        buyersToMoney[buyerChatId], buyer.TinkoffLink));
                    break;
                default:
                    throw new ArgumentException($"Unexpected requisite type {typeRequisites}");
            }
        }

        return $"Тебе нужно заплатить:\n\n{debtMessage}";
    }

    private static bool IsRequisiteValid(string text)
    {
        text = text.Trim();
        var requisites = text.Split();

        if (requisites.Length == 2)
            return IsTelephoneNumberValid(requisites[0]) && IsTinkoffLinkValid(requisites[1]);
        if (requisites.Length != 1)
            return false;

        var phoneAndLink = requisites[0].Split(" ");

        return phoneAndLink.Length switch
        {
            > 2 => false,
            1 => IsTelephoneNumberValid(phoneAndLink[0]),
            2 => IsTelephoneNumberValid(phoneAndLink[0]) && IsTinkoffLinkValid(phoneAndLink[1]),
            _ => false
        };
    }

    private static bool IsTelephoneNumberValid(string telephoneNumber)
    {
        var regex = new Regex(telephoneNumberRegexPattern);
        var matches = regex.Matches(telephoneNumber);
        return matches.Count == 1;
    }

    private static bool IsTinkoffLinkValid(string tinkoffLink)
    {
        var regex = new Regex(tinkoffLinkRegexPattern);
        var matches = regex.Matches(tinkoffLink);
        return matches.Count == 1;
    }

    private static string GetRequisitesAndDebtsMessageText(string buyerUserName, string phoneNumber,
        double money, string? tinkoffLink = null)
        => tinkoffLink == null
            ? string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> —",
                $"\n<b>{Math.Round(money, 1)}руб.</b>\n")
            : string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> \n",
                $"\n<code>{tinkoffLink}</code> \n",
                $"\n<b>{Math.Round(money, 1)}руб.</b>");
}