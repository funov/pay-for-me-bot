using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SqliteProvider.Types;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.KeyboardMarkup;

namespace TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public class PaymentStageMessageHandler : IPaymentStageMessageHandler
{
    private static string[] teamSelectionLabels = { "Создать команду", "Присоединиться к команде" };

    private readonly ILogger<PaymentStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;

    public PaymentStageMessageHandler(
        ILogger<PaymentStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository,
        IBotPhrasesProvider botPhrasesProvider)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;
        this.botPhrasesProvider = botPhrasesProvider;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (!IsUserSentRequisite(chatId))
        {
            var user = userRepository.GetUser(message.Chat.Id);
            var teamId = user.TeamId;

            if (IsRequisiteValid(message.Text!))
            {
                log.LogInformation("Requisite is valid '{messageText}' in chat {chatId} from @{userName}",
                    message.Text, chatId, userName);

                AddPhoneNumberAndTinkoffLink(message.Text!, chatId, teamId);

                if (DoesAllTeamUsersHavePhoneNumber(teamId))
                {
                    var teamUsers2Buyers2Money = GetRequisitesAndDebts(teamId);

                    var teamChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

                    foreach (var teamChatId in teamChatIds)
                    {
                        await SendRequisitesAndDebts(client, teamChatId, cancellationToken,
                            teamUsers2Buyers2Money[teamChatId]);

                        userRepository.ChangeUserStage(chatId, teamId, UserStage.TeamAddition);

                        await client.SendTextMessageAsync(
                            chatId: teamChatId,
                            text: "Создай или присоединись к команде!",
                            replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                            cancellationToken: cancellationToken);
                    }

                    // TODO
                    // constraints (foreign key) 
                    // транзакции BeginTransaction, по очереди все дропает
                    userRepository.DeleteAllUsersByTeamId(teamId);
                    productRepository.DeleteAllProductsByTeamId(teamId);
                    userProductBindingRepository.DeleteAllUserProductBindingsByTeamId(teamId);
                }
                else
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Ждем остальных участников 💤💤💤\n" +
                              "Как только реквизиты отправят все, я рассчитаю чеки и вышлю реквизиты для оплаты 😎😎😎",
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                log.LogInformation("Requisite isn't valid '{messageText}' in chat {chatId} from @{userName}",
                    message.Text, chatId, userName);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты ошибся при отправке реквизитов. Нужно отправить:" +
                          "\n\n <b>Телефон</b> и <b>Ссылку Тинькофф</b> (если есть)" +
                          "\n\n Через пробел или перенос строки 🤓🤓🤓",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
        }

        switch (message.Text!)
        {
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.Help,
                    cancellationToken: cancellationToken);
                return;
        }
    }

    private Dictionary<long, Dictionary<long, double>> GetRequisitesAndDebts(Guid teamId)
    {
        var whomOwesToAmountOwedMoney = new Dictionary<long, Dictionary<long, double>>();
        var teamUserChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

        foreach (var teamUserChatId in teamUserChatIds)
        {
            var productIds = userProductBindingRepository.GetProductBindingsByUserChatId(teamUserChatId, teamId)
                .Select(userProductTable => userProductTable.ProductId).ToList();

            whomOwesToAmountOwedMoney[teamUserChatId] = new Dictionary<long, double>();

            foreach (var productId in productIds)
            {
                var buyerChatId = productRepository.GetBuyerChatId(productId);

                var productPrice = productRepository.GetProductTotalPriceByProductId(productId);

                var amount = productPrice / userProductBindingRepository.GetUserProductBindingCount(productId);

                if (buyerChatId == teamUserChatId)
                    continue;

                if (!whomOwesToAmountOwedMoney[teamUserChatId].ContainsKey(buyerChatId))
                    whomOwesToAmountOwedMoney[teamUserChatId][buyerChatId] = amount;
                else
                    whomOwesToAmountOwedMoney[teamUserChatId][buyerChatId] += amount;
            }
        }

        return whomOwesToAmountOwedMoney;
    }

    private async Task SendRequisitesAndDebts(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken, Dictionary<long, double> buyers2Money)
    {
        var message = MessageForUser(buyers2Money);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        if (message != "Ты никому не должен! 🤩🤩🤩" +
            "\n\n" +
            "Был рад помочь, до встречи!🥰🥰")
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Можешь переходить к оплате. Был рад помочь, до встречи!🥰🥰",
                cancellationToken: cancellationToken);
        }
    }

    private string MessageForUser(Dictionary<long, double> buyersToMoney)
    {
        var message = new StringBuilder();

        if (buyersToMoney.Count == 0)
            return "Ты никому не должен! 🤩🤩🤩" +
                   "\n\n" +
                   "Был рад помочь, до встречи!🥰🥰";

        foreach (var buyerChatId in buyersToMoney.Keys)
        {
            var buyer = userRepository.GetUser(buyerChatId)!;
            var typeRequisites = userRepository.GetRequisiteType(buyerChatId);

            switch (typeRequisites)
            {
                case RequisiteType.PhoneNumber:
                    message.Append(GetRequisitesAndDebtsStringFormat(buyer.Username!, buyer.PhoneNumber!,
                        buyersToMoney[buyerChatId]));
                    break;
                case RequisiteType.PhoneNumberAndTinkoffLink:
                    message.Append(GetRequisitesAndDebtsStringFormat(buyer.Username!, buyer.PhoneNumber!,
                        buyersToMoney[buyerChatId], buyer.TinkoffLink));
                    break;
                default:
                    throw new ArgumentException($"Unexpected requisite type {typeRequisites}");
            }
        }

        return "Ты должен заплатить:\n" + message;
    }

    private bool IsUserSentRequisite(long chatId) => userRepository.IsUserSentRequisite(chatId);

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

    private void AddPhoneNumberAndTinkoffLink(string text, long userChatId, Guid teamId)
    {
        text = text.Trim();
        var requisites = text.Split("\n");
        if (requisites.Length == 2)
        {
            if (IsTelephoneNumberValid(requisites[0]))
            {
                userRepository.AddPhoneNumber(userChatId, requisites[0]);
                userRepository.AddTinkoffLink(userChatId, requisites[1]);
            }
            else
            {
                userRepository.AddPhoneNumber(userChatId, requisites[1]);
                userRepository.AddTinkoffLink(userChatId, requisites[0]);
            }
        }
        else
        {
            var phoneAndLink = text.Split(" ");
            if (phoneAndLink.Length == 1)
            {
                userRepository.AddPhoneNumber(userChatId, phoneAndLink[0]);
            }
            else
            {
                if (IsTelephoneNumberValid(phoneAndLink[0]))
                {
                    userRepository.AddPhoneNumber(userChatId, phoneAndLink[0]);
                    userRepository.AddTinkoffLink(userChatId, phoneAndLink[1]);
                }
                else
                {
                    userRepository.AddPhoneNumber(userChatId, phoneAndLink[1]);
                    userRepository.AddTinkoffLink(userChatId, phoneAndLink[0]);
                }
            }
        }
    }

    private static bool IsTelephoneNumberValid(string telephoneNumber)
    {
        var regex = new Regex(@"^((7|8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$");
        var matches = regex.Matches(telephoneNumber);
        return matches.Count == 1;
    }

    private static bool IsTinkoffLinkValid(string tinkoffLink)
    {
        var regex = new Regex(@"https://www.tinkoff.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+");
        var matches = regex.Matches(tinkoffLink);
        return matches.Count == 1;
    }

    private bool DoesAllTeamUsersHavePhoneNumber(Guid teamId) => userRepository.IsAllTeamHasPhoneNumber(teamId);

    private static string GetRequisitesAndDebtsStringFormat(string buyerUserName, string phoneNumber,
        double money, string? tinkoffLink = null)
        => tinkoffLink == null
            ? string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> —",
                $"\n<b>{Math.Round(money, 1)}руб.</b>\n")
            : string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> \n",
                $"\n<code>{tinkoffLink}</code> \n",
                $"\n<b>{Math.Round(money, 1)}руб.</b>");
}