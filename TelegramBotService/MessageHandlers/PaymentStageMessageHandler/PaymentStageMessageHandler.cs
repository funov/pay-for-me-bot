using Microsoft.Extensions.Logging;
using PaymentLogic.RequisiteParser.RequisiteMessageParser;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.TeamFinisher;

namespace TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public class PaymentStageMessageHandler : IPaymentStageMessageHandler
{
    private readonly ILogger<PaymentStageMessageHandler> log;
    private readonly IUserRepository userRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IRequisiteMessageParser requisiteMessageParser;
    private readonly ITeamFinisher teamFinisher;

    private readonly char[] requisitesSeparators;

    public PaymentStageMessageHandler(
        ILogger<PaymentStageMessageHandler> log,
        IUserRepository userRepository,
        IBotPhrasesProvider botPhrasesProvider,
        IRequisiteMessageParser requisiteMessageParser,
        ITeamFinisher teamFinisher)
    {
        this.log = log;
        this.userRepository = userRepository;
        this.botPhrasesProvider = botPhrasesProvider;
        this.requisiteMessageParser = requisiteMessageParser;
        this.teamFinisher = teamFinisher;

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
                text: botPhrasesProvider.Help,
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

        if (requisiteMessageParser.IsRequisiteValid(messageText))
        {
            log.LogInformation("Requisite is valid '{messageText}' in chat {chatId} from @{userName}",
                messageText, chatId, userName);

            AddPhoneNumberAndTinkoffLink(messageText, chatId);

            if (userRepository.IsAllTeamHasPhoneNumber(teamId))
            {
                await teamFinisher.FinishTeamAsync(client, teamId, cancellationToken);
                return;
            }

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.WaitingOtherUsersRequisites,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        log.LogInformation("Requisite isn't valid '{messageText}' in chat {chatId} from @{userName}",
            messageText, chatId, userName);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: botPhrasesProvider.RequisitesSendingError,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
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
                if (requisiteMessageParser.PhoneNumbersVerifier.IsPhoneNumberValid(requisites[0]))
                    userRepository.AddPhoneNumber(chatId, requisites[0]);
                else if (requisiteMessageParser.PhoneNumbersVerifier.IsPhoneNumberValid(requisites[1]))
                    userRepository.AddPhoneNumber(chatId, requisites[1]);

                if (requisiteMessageParser.BankingLinkVerifier.IsBankingLinkValid(requisites[0]))
                    userRepository.AddTinkoffLink(chatId, requisites[0]);
                else if (requisiteMessageParser.BankingLinkVerifier.IsBankingLinkValid(requisites[1]))
                    userRepository.AddTinkoffLink(chatId, requisites[1]);

                break;
            }
        }
    }
}