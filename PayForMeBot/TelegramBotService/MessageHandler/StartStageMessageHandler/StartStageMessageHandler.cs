using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;

public class StartStageMessageHandler : IStartStageMessageHandler
{
    private static string[] teamSelectionLabels = { "Создать команду", "Присоединиться к команде" };

    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    private static string HelpMessage
        => "❓❓❓\n\n1) Для начала нужно либо создать команду, либо вступить в существующую. 🤝🤝🤝\n\n" +
           "2) Далее каждого попросят ввести <b>номер телефона</b> и <b>ссылку Тинькофф</b> (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт/услугу количествов штуках и цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар. Ты увидишь «✅». Для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось, и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «<b>Перейти к разделению счёта</b>💴». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

    public StartStageMessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IKeyboardMarkup keyboardMarkup,
        IDbDriver dbDriver)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.dbDriver = dbDriver;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        // TODO Брать их из массива (teamSelectionLabels)
        // TODO Добавить ограничение завершения только на лидера группы
        // TODO Рефакторинг

        switch (message.Text!)
        {
            case "/start":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Создай или присоединись к команде!",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(teamSelectionLabels),
                    cancellationToken: cancellationToken);
                break;
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: HelpMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;
            case "Создать команду":
                if (!IsUserInTeam(chatId))
                {
                    var userTeamId = Guid.NewGuid();

                    log.LogInformation("@{username} created a team {guid} in chat {chatId}",
                        userName, userTeamId, chatId);

                    dbDriver.AddUser(message.Chat.Username!, chatId, userTeamId);

                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Код вашей комманды:\n\n<code>{userTeamId}</code>",
                        parseMode: ParseMode.Html,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken
                    );

                    dbDriver.ChangeUserStage(chatId, userTeamId, "middle");
                }
                else
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Ты уже в команде!",
                        cancellationToken: cancellationToken
                    );
                    break;
                }

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Можешь начинать писать продукты!" +
                          $"\n\n" +
                          $"Когда закончишь вводить/выбирать продукты, нажми на кнопку внизу ⬇",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] { "Перейти к разделению счёта💴" }),
                    cancellationToken: cancellationToken
                );
                break;
            case "Присоединиться к команде":
                if (!IsUserInTeam(chatId))
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Отправь мне код вашей команды",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken
                    );
                    break;
                }
                else
                {
                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Ты уже в команде!",
                        cancellationToken: cancellationToken
                    );
                    break;
                }
        }

        // TODO Дыра, что чел может обойти все это и просто скинуть Guid и присоединиться к команде

        if (Guid.TryParse(message.Text, out var teamId))
        {
            log.LogInformation("@{username} joined team {guid} in {chatId}",
                userName, teamId, chatId);

            dbDriver.AddUser(userName, chatId, teamId);
            dbDriver.ChangeUserStage(chatId, teamId, "middle");

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"Можешь начинать писать продукты!" +
                      $"\n\n" +
                      $"Когда закончишь вводить/выбирать продукты, нажми на кнопку внизу ⬇",
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] { "Перейти к разделению счёта💴" }),
                cancellationToken: cancellationToken
            );

            var pastProducts = dbDriver.GetProductsByTeamId(teamId);

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

    private bool IsUserInTeam(long userChatId) => dbDriver.IsUserInDb(userChatId);
}