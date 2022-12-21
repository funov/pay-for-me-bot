using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotService.KeyboardMarkup;

namespace TelegramBotService.TelegramBotService.MessageHandlers.PaymentStageMessageHandler;

public class PaymentStageMessageHandler : IPaymentStageMessageHandler
{
    private static string[] teamSelectionLabels = { "Создать команду", "Присоединиться к команде" };

    private readonly ILogger<PaymentStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;

    private static string HelpMessage
        => "❓❓❓\n\n1) Для начала нужно либо создать команду, либо вступить в существующую. 🤝🤝🤝\n\n" +
           "2) При создании команды бот пришлет уникальный код команды. Этот код должен ввести каждый участник при присоединении." +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт/услугу количествов штуках и цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар. Ты увидишь «✅». Для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось, и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «<b>Перейти к разделению счёта</b>💴».\n\n" +
           "5) Далее каждого попросят ввести <b>номер телефона</b> и <b>ссылку Тинькофф</b> (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\nПотом бот разошлет всем реквизиты и суммы для переводов 🎉🎉🎉";

    public PaymentStageMessageHandler(
        ILogger<PaymentStageMessageHandler> log,
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (!IsUserSentRequisite(chatId))
        {
            var teamId = userRepository.GetTeamIdByUserChatId(message.Chat.Id);

            if (IsRequisiteValid(message.Text!))
            {
                log.LogInformation("Requisite is valid '{messageText}' in chat {chatId} from @{userName}",
                    message.Text, chatId, userName);

                AddPhoneNumberAndTinkoffLink(message.Text!, chatId, teamId);

                if (DoesAllTeamUsersHavePhoneNumber(teamId))
                {
                    var teamUsers2Buyers2Money = GetRequisitesAndDebts(teamId);

                    var teamChatIds = userRepository.GetUsersChatIdInTeam(teamId);

                    foreach (var teamChatId in teamChatIds)
                    {
                        await SendRequisitesAndDebts(client, teamChatId, cancellationToken,
                            teamUsers2Buyers2Money[teamChatId]);

                        userRepository.ChangeUserStage(chatId, teamId, "start");

                        await client.SendTextMessageAsync(
                            chatId: teamChatId,
                            text: "Создай или присоединись к команде!",
                            replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                            cancellationToken: cancellationToken);
                    }

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
                    text: HelpMessage,
                    cancellationToken: cancellationToken);
                return;
        }
    }

    public Dictionary<long, Dictionary<long, double>> GetRequisitesAndDebts(Guid teamId)
    {
        var whomOwesToAmountOwedMoney = new Dictionary<long, Dictionary<long, double>>();
        var teamUserChatIds = userRepository.GetUsersChatIdInTeam(teamId);

        foreach (var teamUserChatId in teamUserChatIds)
        {
            var productIds = userProductBindingRepository.GetProductBindingsByUserChatId(teamUserChatId, teamId)
                .Select(userProductTable => userProductTable.ProductId).ToList();

            whomOwesToAmountOwedMoney[teamUserChatId] = new Dictionary<long, double>();

            foreach (var productId in productIds)
            {
                var buyerChatId = productRepository.GetBuyerChatId(productId);

                var productPrice = productRepository.GetTotalPriceByProductId(productId);

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

    private string MessageForUser(Dictionary<long, double> buyers2Money)
    {
        var message = new StringBuilder();

        if (buyers2Money.Count == 0)
            return "Ты никому не должен! 🤩🤩🤩" +
                   "\n\n" +
                   "Был рад помочь, до встречи!🥰🥰";

        foreach (var value in buyers2Money)
        {
            var buyerUsername = userRepository.GetUsernameByChatId(value.Key);
            var typeRequisites = userRepository.GetTypeRequisites(value.Key);
            var phoneNumber = userRepository.GetPhoneNumberByChatId(value.Key);

            if (typeRequisites == "phoneNumber")
            {
                message.Append(GetRequisitesAndDebtsStringFormat(buyerUsername, phoneNumber, value.Value));
            }

            if (typeRequisites == "tinkoffLink")
            {
                var tinkoffLink = userRepository.GetTinkoffLinkByUserChatId(value.Key);
                message
                    .Append(GetRequisitesAndDebtsStringFormat(buyerUsername, phoneNumber, value.Value, tinkoffLink!));
            }
        }

        return "Ты должен заплатить:\n" + message;
    }

    private bool IsUserSentRequisite(long chatId) => userRepository.IsUserSentRequisite(chatId);

    private bool IsRequisiteValid(string text)
    {
        text = text.Trim();
        var requisites = text.Split();

        if (requisites.Length != 2)
        {
            if (requisites.Length == 1)
            {
                var phoneAndLink = requisites[0].Split(" ");
                if (phoneAndLink.Length > 2)
                    return false;
                else
                {
                    if (phoneAndLink.Length == 1)
                        return IsTelephoneNumberValid(phoneAndLink[0]);
                    if (phoneAndLink.Length == 2)
                        return IsTelephoneNumberValid(phoneAndLink[0]) && IsTinkoffLinkValid(phoneAndLink[1]);
                }
            }

            return false;
        }

        return IsTelephoneNumberValid(requisites[0]) && IsTinkoffLinkValid(requisites[1]);
    }

    private void AddPhoneNumberAndTinkoffLink(string text, long userChatId, Guid teamId)
    {
        text = text.Trim();
        var requisites = text.Split("\n");
        if (requisites.Length == 2)
        {
            if (IsTelephoneNumberValid(requisites[0]))
            {
                userRepository.AddPhoneNumberAndTinkoffLink(userChatId, teamId, requisites[0], requisites[1]);
            }
            else
            {
                userRepository.AddPhoneNumberAndTinkoffLink(userChatId, teamId, requisites[1], requisites[0]);
            }
        }
        else
        {
            var phoneAndLink = text.Split(" ");
            if (phoneAndLink.Length == 1)
            {
                userRepository.AddPhoneNumberAndTinkoffLink(userChatId, teamId, phoneAndLink[0]);
            }
            else
            {
                if (IsTelephoneNumberValid(phoneAndLink[0]))
                {
                    userRepository.AddPhoneNumberAndTinkoffLink(userChatId, teamId, phoneAndLink[0], phoneAndLink[1]);
                }
                else
                {
                    userRepository.AddPhoneNumberAndTinkoffLink(userChatId, teamId, phoneAndLink[1], phoneAndLink[0]);
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