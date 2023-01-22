using System.Text;
using PaymentLogic.DebtsCalculator;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Transactions.DeleteAllTeamIdTransaction;
using SqliteProvider.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.ButtonUtils.KeyboardMarkup;

namespace TelegramBotService.TeamFinisher;

public class TeamFinisher : ITeamFinisher
{
    private readonly IDeleteAllTeamIdTransaction deleteAllTeamIdTransaction;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IUserRepository userRepository;
    private readonly IDebtsCalculator debtsCalculator;

    private readonly string[] teamSelectionLabels;

    public TeamFinisher(
        IDeleteAllTeamIdTransaction deleteAllTeamIdTransaction,
        IDebtsCalculator debtsCalculator,
        IKeyboardMarkup keyboardMarkup,
        IBotPhrasesProvider botPhrasesProvider,
        IUserRepository userRepository)
    {
        this.deleteAllTeamIdTransaction = deleteAllTeamIdTransaction;
        this.debtsCalculator = debtsCalculator;
        this.keyboardMarkup = keyboardMarkup;
        this.botPhrasesProvider = botPhrasesProvider;
        this.userRepository = userRepository;

        teamSelectionLabels = new[] { botPhrasesProvider.CreateTeamButton, botPhrasesProvider.JoinTeamButton };
    }

    public async Task FinishTeamAsync(
        ITelegramBotClient client,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var teamChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

        await CompleteTeamsAndSendDebtsAsync(client, teamChatIds, teamId, cancellationToken);

        deleteAllTeamIdTransaction.DeleteAllTeamId(teamId);
    }

    private async Task CompleteTeamsAndSendDebtsAsync(ITelegramBotClient client, IEnumerable<long> teamChatIds,
        Guid teamId, CancellationToken cancellationToken)
    {
        var userIdToBuyerIdToDebt = debtsCalculator.GetUserIdToBuyerIdToDebt(teamId);

        foreach (var teamChatId in teamChatIds)
        {
            await SendRequisitesAndDebtsAsync(client, teamChatId, userIdToBuyerIdToDebt[teamChatId], cancellationToken);

            await client.SendTextMessageAsync(
                chatId: teamChatId,
                text: botPhrasesProvider.CreateOrJoinTeam,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                cancellationToken: cancellationToken);
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
                text: botPhrasesProvider.Goodbye,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }

    private string GetDebtMessageText(Dictionary<long, double> buyersToMoney)
    {
        var debtMessage = new StringBuilder();

        if (buyersToMoney.Count == 0)
            return botPhrasesProvider.WithoutDebt;

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
            ? string.Join("\n", $"@{buyerUserName}", $"<code>{phoneNumber}</code>",
                $"\n<b>{Math.Round(money, 1)}руб.</b>\n\n")
            : string.Join("\n", $"@{buyerUserName}", $"<code>{phoneNumber}</code>",
                $"<code>{tinkoffLink}</code>",
                $"\n<b>{Math.Round(money, 1)}руб.</b>\n\n");
}