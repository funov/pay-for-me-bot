using System.Text;
using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot.Types.ReplyMarkups;
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
           "2) Далее каждого попросят ввести <b>номер телефона</b> и <b>ссылку Тинькофф</b> (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт/услугу количествов штуках и цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар. Ты увидишь «✅». Для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось, и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «<b>Перейти к разделению счёта</b>💴». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

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

        // Ждем сразу телефон и тиньк ссылку
        // Но перед этим смотрим в бд есть ли это у юзера, если есть то игнорим
        if (!IsUserSentRequisite(chatId))
        {
            var teamId = dbDriver.GetTeamIdByUserChatId(message.Chat.Id);
            if (IsRequisiteValid(message.Text!))
            {
                AddPhoneNumberAndTinkoffLink(message.Text!, chatId, teamId);
                dbDriver.UserIsReady(chatId);
                
                if (dbDriver.IsTeamReady(dbDriver.GetTeamIdByUserChatId(chatId)))
                {
                    var teamUsers = dbDriver.GetUsersChatIdInTeam(teamId);
                    foreach (var teamUser in teamUsers)
                    {
                        await SendRequisitesAndDebts(client, teamUser, cancellationToken);
                    }
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Был рад помочь, до встречи!",
                        cancellationToken: cancellationToken);
                    dbDriver.ChangeUserStage(chatId, teamId, "start");
                }
                else
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Ждем остальных участников." +
                              "Как только все отправят, я рассчитаю чеки и вышлю реквизиты",
                        cancellationToken: cancellationToken);
                }
            }

            else
            {
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты отправил неверные реквизиты, попробуй еще раз",
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
        CancellationToken cancellationToken)
    {
        var teamId = dbDriver.GetTeamIdByUserChatId(chatId);
        var buyers2Money = dbDriver.GetRequisitesAndDebts(chatId, teamId);

        if (AllTeamUsersHavePhoneNumber(teamId))
        {
            var message = MessageForUser(buyers2Money);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: cancellationToken
            );

            dbDriver.ChangeUserStage(chatId, teamId, "start");

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Можешь переходить к оплате. Обязательно возвращайся в следующий раз! 🥰🥰",
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Кто-то не указал свой номер телефона, его нужно ввести и попробовать снова",
                cancellationToken: cancellationToken
            );
        }
    }

    private string MessageForUser(Dictionary<long, double> buyers2Money)
    {
        var message = new StringBuilder();
        foreach (var pair in buyers2Money)
        {
            var buyerUserName = dbDriver.GetUsernameByChatId(pair.Key);
            var typeRequisites = dbDriver.GetTypeRequisites(pair.Key);
            if (typeRequisites == "phoneNumber")
            {
                var phoneNumber = dbDriver.GetPhoneNumberByChatId(pair.Key);
                message.Append(StringFormat(buyerUserName, phoneNumber, pair.Value));
            }

            if (typeRequisites == "tinkoffLink")
            {
                var tinkoffLink = dbDriver.GetTinkoffLinkByUserChatId(pair.Key);
                message.Append(StringFormat(buyerUserName, tinkoffLink!, pair.Value));
            }
        }

        return "Ты должен заплатить:\n" + message;
    }

    private bool IsUserSentRequisite(long chatId) => dbDriver.IsUserSentRequisite(chatId);

    private bool IsRequisiteValid(string text)
    {
        text = text.Trim();
        var requisites = text.Split("\n");
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
        var regex = new Regex(@"https://www.tinkof.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+");
        var matches = regex.Matches(tinkoffLink);
        return matches.Count == 1;
    }

    private bool AllTeamUsersHavePhoneNumber(Guid teamId) => dbDriver.DoesAllTeamUsersHavePhoneNumber(teamId);

    private static string StringFormat(string buyerUserName, string requisites, double money)
        => new StringBuilder()
            .Append("@" + buyerUserName + " ")
            .Append(requisites + ": ")
            .Append(money + "руб.\n")
            .ToString();
}