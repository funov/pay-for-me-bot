using Microsoft.Extensions.Logging;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotService.KeyboardMarkup;

namespace TelegramBotService.TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

public class TeamAdditionStageMessageHandler : ITeamAdditionStageMessageHandler
{
    private static string[] teamSelectionLabels = {"Создать команду", "Присоединиться к команде"};

    private readonly ILogger<TeamAdditionStageMessageHandler> log;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;

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

    public TeamAdditionStageMessageHandler(
        ILogger<TeamAdditionStageMessageHandler> log, 
        IKeyboardMarkup keyboardMarkup,
        IUserRepository userRepository,
        IProductRepository productRepository)
    {
        this.log = log;
        this.keyboardMarkup = keyboardMarkup;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userName = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, userName);

        // TODO Добавить ограничение завершения только на лидера группы
        // TODO Рефакторинг

        switch (message.Text!)
        {
            case "/start":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Создай команду или присоединись к ней!",
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

                    userRepository.AddUser(message.Chat.Username!, chatId, userTeamId);

                    await client.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Код вашей комманды:\n\n<code>{userTeamId}</code>",
                        parseMode: ParseMode.Html,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken
                    );

                    userRepository.ChangeUserStage(chatId, userTeamId, "middle");
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
                    text: $"Можешь начинать добавлять продукты!" +
                          $"\n\n" +
                          "Можешь прислать мне чек с продуктами, на котором хорошо виден куар-код." +
                          "\n\n" +
                          "Если чека нет, то можешь прислать мне сообщение с товаром, количеством и общей ценой." +
                          "\n\n" +
                          "Например, вишневый пирог 5 399.99" +
                          "\n\n" +
                          "Или такси до центра 1 500" +
                          "\n\n" +
                          "Я тут же пришлю товар/товары всем участинкам команды" +
                          "\n\n" +
                          $"Когда закончишь вводить/выбирать продукты, нажми на кнопку внизу ⬇",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] {"Перейти к разделению счёта💴"}),
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

            userRepository.AddUser(userName, chatId, teamId);
            userRepository.ChangeUserStage(chatId, teamId, "middle");

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"Можешь начинать добавлять продукты!" +
                      $"\n\n" +
                      "Можешь прислать мне чек с продуктами, на котором хорошо виден куар-код." +
                      "\n\n" +
                      "Если чека нет, то можешь прислать мне сообщение с товаром, количеством и общей ценой." +
                      "\n\n" +
                      "Например, вишневый пирог 5 399.99" +
                      "\n\n" +
                      "Или такси до центра 1 500" +
                      "\n\n" +
                      "Я тут же пришлю товар/товары всем участинкам команды" +
                      "\n\n" +
                      $"Когда закончишь вводить/выбирать продукты, нажми на кнопку внизу ⬇",
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] {"Перейти к разделению счёта💴"}),
                cancellationToken: cancellationToken
            );

            var pastProducts = productRepository.GetProductsByTeamId(teamId);

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

    private bool IsUserInTeam(long userChatId) => userRepository.IsUserInDb(userChatId);
}