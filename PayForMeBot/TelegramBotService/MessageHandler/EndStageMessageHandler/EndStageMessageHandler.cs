using System.Text;
using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;

namespace PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;

public class EndStageMessageHandler : IEndStageMessageHandler
{
    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    private static string HelpMessage
        => "❓❓❓\n\n1) Для начала нужно либо создать команду, либо вступить в существующую. 🤝🤝🤝\n\n" +
           "2) Далее каждого попросят ввести номер телефона и ссылку Тинькофф (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт и его цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар, если все хорошо, ты увидишь «✅», для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «Завершить». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);

        switch (message.Text!)
        {
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: HelpMessage,
                    cancellationToken: cancellationToken);
                return;
            case "Готово":
                if (!IsUserSentRequisite(chatId))
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Отправь свои реквизиты: " +
                              "номер телефона и/или ссылку на Тинькофф, если все в команде используют Тинькофф банк",
                        cancellationToken: cancellationToken);
                    var teamId = dbDriver.GetTeamIdByUserChatId(message.Chat.Id);
                    IsRequisiteValid(message.Text!);
                    //dbDriver.AddTelephoneNumberAndTinkoffLink(message.Chat.Id, teamId, );
                    return;
                }

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты уже нажал Готово, твои реквизиты приняты",
                    cancellationToken: cancellationToken);
                return;
        }

        if (IsRequisiteValid(message.Text!))
        {
            // db.AddReceiveMoneyMethod(...);
            log.LogInformation("User sent valid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ждем остальных участников. Как только все отправят, " +
                      "я рассчитаю чеки и вышлю реквизиты",
                cancellationToken: cancellationToken);
        }
        else
        {
            log.LogInformation("User sent invalid rm method {method}", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Ты скинул неправильные реквизиты. " +
                      "Отправь мне ссылку на Тинькофф и/или номер телефона еще раз",
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendRequisitesAndDebts(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken)
    {
        var teamId = dbDriver.GetTeamIdByUserChatId(chatId);
        var buyers2Money = dbDriver.GetRequisitesAndDebts(chatId, teamId);

        if (AllTeamUsersHavePhoneNumber(teamId))
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: MessageForUser(buyers2Money),
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Кто-то не указал свой номер телефон, его нужно ввести и попробовать снова",
                cancellationToken: cancellationToken
            );
        }
    }

    private string MessageForUser(Dictionary<long, double> buyers2Money)
    {
        var b = new StringBuilder();
        foreach (var pair in buyers2Money)
        {
            var buyerUserName = dbDriver.GetUsernameByChatId(pair.Key);
            var typeRequisites = dbDriver.GetTypeRequisites(pair.Key);
            if (typeRequisites == "phoneNumber")
            {
                var phoneNumber = dbDriver.GetPhoneNumberByChatId(pair.Key);
                b.Append(String.Format("{buyerUserName} {phoneNumber}: {money}\n",
                    buyerUserName, phoneNumber, pair.Value));
            }

            if (typeRequisites == "tinkoffLink")
            {
                var tinkoffLink = dbDriver.GetTinkoffLinkByUserChatId(pair.Key);
                b.Append(String.Format("{buyerUserName} {phoneNumber}: {money}",
                    buyerUserName, tinkoffLink, pair.Value));
            }
        }

        return "Ты должен заплатить:\n" + b;
    }
    

    private bool IsUserSentRequisite(long chatId) => dbDriver.IsUserSentRequisite(chatId);

    private bool IsRequisiteValid(string text)
    {
        var requisites = text.Split("\n");
        if (requisites.Length != 2)
        {
            if (requisites.Length == 1)
                return IsTelephoneNumberValid(requisites[0]);
            return false;
        }
        return IsTelephoneNumberValid(requisites[0]) && IsTinkoffLinkValid(requisites[1]);
    }

    private bool IsTelephoneNumberValid(string telephoneNumber)
    {
        var regex = new Regex(@"^((8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$");
        var matches = regex.Matches(telephoneNumber);
        if (matches.Count == 1)
            return true;
        return false;
    }

    private bool IsTinkoffLinkValid(string tinkoffLink)
    {
        var regex = new Regex(@"https://www.tinkof.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+");
        var matches = regex.Matches(tinkoffLink);
        if (matches.Count == 1)
            return true;
        return false;
    }

    private bool AllTeamUsersHavePhoneNumber(Guid teamId) => dbDriver.DoesAllTeamUsersHavePhoneNumber(teamId);
}    