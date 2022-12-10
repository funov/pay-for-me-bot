﻿using Microsoft.Extensions.Logging;
using PayForMeBot.DbDriver;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;
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
           "2) Далее каждого попросят ввести номер телефона и ссылку Тинькофф (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт и его цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар, если все хорошо, ты увидишь «✅», для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «Завершить». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

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

        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);

        // TODO Брать их из массива (teamSelectionLabels)
        // TODO Подсчитать расходы и скинуть ссылки каждому
        // TODO Добавить ограничение завершения только на лидера группы
        // TODO рефакторинг

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
                    cancellationToken: cancellationToken);
                break;
            case "Создать команду":
                var userTeamId = new Guid();
                log.LogInformation("{username} created team {guid} in {chatId}",
                    message.Chat.Username, userTeamId, chatId);

                dbDriver.AddUser(message.Chat.Username!, chatId, userTeamId, null);

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Код вашей комманды:\n```{userTeamId}```",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );

                dbDriver.ChangeUserState(chatId, userTeamId, "middle");

                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Можете начинать писать продукты!",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
            case "Присоединиться к команде":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Отправь код вашей команды",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
        }

        if (Guid.TryParse(message.Text, out var teamId))
        {
            log.LogInformation("{username} joined team {guid} in {chatId}",
                message.Chat.Username, teamId, chatId);

            dbDriver.AddUser(message.Chat.Username!, chatId, teamId, null);
            dbDriver.ChangeUserState(chatId, teamId, "middle");

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"Можете начинать писать продукты!",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );
        }
    }
}