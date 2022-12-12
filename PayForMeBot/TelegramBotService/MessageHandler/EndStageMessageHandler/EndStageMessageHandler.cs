using System.Text;
using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot.Types.Enums;

namespace PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;

public class EndStageMessageHandler : IEndStageMessageHandler
{
    private static string[] teamSelectionLabels = { "Создать команду", "Присоединиться к команде" };

    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IDbDriver dbDriver;
    private readonly IKeyboardMarkup keyboardMarkup;

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

    public EndStageMessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IDbDriver dbDriver,
        IKeyboardMarkup keyboardMarkup)
    {
        this.log = log;
        this.dbDriver = dbDriver;
        this.keyboardMarkup = keyboardMarkup;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        if (!IsUserSentRequisite(chatId))
        {
            var teamId = dbDriver.GetTeamIdByUserChatId(message.Chat.Id);

            if (IsRequisiteValid(message.Text!))
            {
                log.LogInformation("Requisite is valid '{messageText}' in chat {chatId} from @{userName}",
                    message.Text, chatId, userName);

                AddPhoneNumberAndTinkoffLink(message.Text!, chatId, teamId);

                if (DoesAllTeamUsersHavePhoneNumber(teamId))
                {
                    var teamUsers2Buyers2Money = dbDriver.GetRequisitesAndDebts(teamId);

                    var teamChatIds = dbDriver.GetUsersChatIdInTeam(teamId);

                    foreach (var teamChatId in teamChatIds)
                    {
                        await SendRequisitesAndDebts(client, teamChatId, cancellationToken,
                            teamUsers2Buyers2Money[teamChatId]);

                        dbDriver.ChangeUserStage(chatId, teamId, "start");

                        await client.SendTextMessageAsync(
                            chatId: teamChatId,
                            text: "Создай или присоединись к команде!",
                            replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                            cancellationToken: cancellationToken);
                    }

                    dbDriver.DeleteTeamInDb(teamId);
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

        if (message != "Ты никому не должен! 🤩🤩🤩")
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
            return "Ты никому не должен! 🤩🤩🤩";
        
        foreach (var value in buyers2Money)
        {
            var buyerUsername = dbDriver.GetUsernameByChatId(value.Key);
            var typeRequisites = dbDriver.GetTypeRequisites(value.Key);
            var phoneNumber = dbDriver.GetPhoneNumberByChatId(value.Key);

            if (typeRequisites == "phoneNumber")
            {
                message.Append(GetRequisitesAndDebtsStringFormat(buyerUsername, phoneNumber, value.Value));
            }

            if (typeRequisites == "tinkoffLink")
            {
                var tinkoffLink = dbDriver.GetTinkoffLinkByUserChatId(value.Key);
                message
                    .Append(GetRequisitesAndDebtsStringFormat(buyerUsername, phoneNumber, value.Value, tinkoffLink!));
            }
        }

        return "Ты должен заплатить:\n" + message;
    }

    private bool IsUserSentRequisite(long chatId) => dbDriver.IsUserSentRequisite(chatId);

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
        if (requisites.Length != 2)
        {
            dbDriver.AddPhoneNumberAndTinkoffLink(userChatId, teamId, text);
        }
        else
        {
            var phoneAndLink = text.Split(" ");
            if (IsTelephoneNumberValid(phoneAndLink[0]))
            {
                dbDriver.AddPhoneNumberAndTinkoffLink(userChatId, teamId, phoneAndLink[0], phoneAndLink[1]);
            }
            else
            {
                dbDriver.AddPhoneNumberAndTinkoffLink(userChatId, teamId, phoneAndLink[1], phoneAndLink[0]);
            }
        }
    }

    private static bool IsTelephoneNumberValid(string telephoneNumber)
    {
        var regex = new Regex(@"^((8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$");
        var matches = regex.Matches(telephoneNumber);
        return matches.Count == 1;
    }

    private static bool IsTinkoffLinkValid(string tinkoffLink)
    {
        var regex = new Regex(@"https://www.tinkoff.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+");
        var matches = regex.Matches(tinkoffLink);
        return matches.Count == 1;
    }

    private bool DoesAllTeamUsersHavePhoneNumber(Guid teamId) => dbDriver.DoesAllTeamUsersHavePhoneNumber(teamId);

    private static string GetRequisitesAndDebtsStringFormat(string buyerUserName, string phoneNumber,
        double money, string? tinkoffLink = null)
        => tinkoffLink == null
            ? string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code> —", $"\n{money}руб.\n")
            : string.Join(" ", $"@{buyerUserName}", $"<code>{phoneNumber}</code>,",
                $"\n<code>{tinkoffLink}</code> ",
                $"\n{Math.Round(money, 1)}руб.\n");
}