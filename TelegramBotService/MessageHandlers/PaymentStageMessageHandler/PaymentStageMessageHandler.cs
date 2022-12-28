using System.Text;
using PaymentLogic;
using Microsoft.Extensions.Logging;
using PaymentLogic.RequisiteParser.BankingLinkVerifier;
using PaymentLogic.RequisiteParser.BankingLinkVerifier.Implementations;
using PaymentLogic.RequisiteParser.RequisiteMessagePaesser;
using PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier;
using PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier.Implementations;
using SqliteProvider.Types;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Transactions.DeleteAllTeamIdTransaction;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.ButtonUtils.KeyboardMarkup;

namespace TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public class PaymentStageMessageHandler : IPaymentStageMessageHandler
{
    // TODO подумать, а вдруг тут можно отказаться от спецификации и нужное протягивать в DI
    private static TelePhoneNumberVerifier telePhoneNumbersVerifier = new RussianTelePhoneNumberVerifier();
    private static BankingLinkVerifier bankingLinkVerifier = new TinkoffLinkVerifier();

    private readonly ILogger<PaymentStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IDebtsCalculator debtsCalculator;
    private readonly IDeleteAllTeamIdTransaction deleteAllTeamIdTransaction;

    private readonly string?[] teamSelectionLabels;
    private readonly char[] requisitesSeparators;

    public PaymentStageMessageHandler(
        ILogger<PaymentStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository,
        IBotPhrasesProvider botPhrasesProvider,
        IDebtsCalculator debtsCalculator,
        IDeleteAllTeamIdTransaction deleteAllTeamIdTransaction)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;
        this.botPhrasesProvider = botPhrasesProvider;
        this.debtsCalculator = debtsCalculator;
        this.deleteAllTeamIdTransaction = deleteAllTeamIdTransaction;

        teamSelectionLabels = new[] {botPhrasesProvider.CreateTeamButton, botPhrasesProvider.JoinTeamButton};
        requisitesSeparators = new[] {'\n', ' '};
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

        if (RequisiteMessageParser.IsRequisiteValid(messageText,
                telePhoneNumbersVerifier,
                bankingLinkVerifier))
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
        var teamChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

        await CompleteTeamsAndSendDebtsAsync(client, teamChatIds, teamId, chatId, cancellationToken);

        deleteAllTeamIdTransaction.DeleteAllTeamId(teamId);
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

    private async Task CompleteTeamsAndSendDebtsAsync(ITelegramBotClient client, IEnumerable<long> teamChatIds,
        Guid teamId, long chatId, CancellationToken cancellationToken)
    {
        var userIdToBuyerIdToDebt = debtsCalculator.GetUserIdToBuyerIdToDebt(teamId);

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
                if (telePhoneNumbersVerifier.IsTelePhoneNumberValid(requisites[0]))
                    userRepository.AddPhoneNumber(chatId, requisites[0]);
                else if (telePhoneNumbersVerifier.IsTelePhoneNumberValid(requisites[1]))
                    userRepository.AddPhoneNumber(chatId, requisites[1]);

                if (bankingLinkVerifier.IsBankingLinkValid(requisites[0]))
                    userRepository.AddTinkoffLink(chatId, requisites[0]);
                else if (bankingLinkVerifier.IsBankingLinkValid(requisites[1]))
                    userRepository.AddTinkoffLink(chatId, requisites[1]);

                break;
            }
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

    private static string GetRequisitesAndDebtsMessageText(string buyerUserName, string phoneNumber,
        double money, string? tinkoffLink = null)
        => tinkoffLink == null
            ? string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> —",
                $"\n<b>{Math.Round(money, 1)}руб.</b>\n")
            : string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> \n",
                $"\n<code>{tinkoffLink}</code> \n",
                $"\n<b>{Math.Round(money, 1)}руб.</b>");
}