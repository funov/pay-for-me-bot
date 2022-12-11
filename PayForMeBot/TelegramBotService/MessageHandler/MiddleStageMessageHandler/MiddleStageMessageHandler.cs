using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PayForMeBot.DbDriver;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PayForMeBot.TelegramBotService.MessageHandler.MiddleStageMessageHandler;

public class MiddleStageMessageHandler : IMiddleStageMessageHandler
{
    private static string HelpMessage
        => "❓❓❓\n\n1) Для начала нужно либо создать команду, либо вступить в существующую. 🤝🤝🤝\n\n" +
           "2) Далее каждого попросят ввести номер телефона и ссылку Тинькофф (если есть) для " +
           "того, чтобы тебе смогли перевести деньги. 🤑🤑🤑\n\n" +
           "3) Теперь можно начать вводить товары или услуги. Напиши продукт/услугу количествов штуках и цену, либо просто отправь чек " +
           "(где хорошо виден QR-код). Далее нажми на «🛒», чтобы позже заплатить " +
           "за этот товар. Ты увидишь «✅». Для отмены нажми еще раз на эту кнопку. 🤓🤓🤓\n\n" +
           "4) Если ваше мероприятие закончилось, и вы выбрали за что хотите платить, кто-то должен нажать " +
           "на кнопку «Перейти к разделению счёта💴». Дальше всем придут суммы и реквизиты для переводов. 🎉🎉🎉";

    private readonly ILogger<ReceiptApiClient.ReceiptApiClient> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IDbDriver dbDriver;

    public MiddleStageMessageHandler(ILogger<ReceiptApiClient.ReceiptApiClient> log, IReceiptApiClient receiptApiClient,
        IKeyboardMarkup keyboardMarkup, IDbDriver dbDriver)
    {
        this.log = log;
        this.receiptApiClient = receiptApiClient;
        this.keyboardMarkup = keyboardMarkup;
        this.dbDriver = dbDriver;
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        log.LogInformation("Received a '{messageText}' message in chat {chatId}", message.Text, chatId);
        var teamId = dbDriver.GetTeamIdByUserChatId(chatId);

        
        
        switch (message.Text!)
        {
            // TODO Добавить ограничение завершения только на лидера группы
            // TODO рефакторинг

            case "Перейти к разделению счёта💴":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты уверен, что все участники команды уже готовы делить счет?" +
                          "\n\n" +
                          "После этого бот подсчитает кто кому сколько должен и скинет реквизиты для оплаты" +
                          "\n\n" +
                          "Вернуться к выбору продуктов будет невозможно!",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] {"Да", "Нет"}),
                    cancellationToken: cancellationToken);
                return;
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: HelpMessage,
                    cancellationToken: cancellationToken);
                return;
            case "Да":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Отправь мне свой номер телефона и ссылку на реквизиты в Тинькофф банк (если она есть).",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                dbDriver.ChangeUserStage(chatId, teamId, "end");
                return;
            case "Нет":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Нажми, как будете готовы делить счет!",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new []{"Перейти к разделению счёта💴"}),
                    cancellationToken: cancellationToken);
                break;
        }

        if (Product.TryParse(message.Text!, out var dbProduct))
        {
            var productGuid = Guid.NewGuid();

            dbDriver.AddProduct(productGuid, dbProduct, productGuid, chatId, teamId);

            log.LogInformation("User added {product} with cost {price} in chat {chatId}",
                dbProduct.Name, dbProduct.Price, chatId);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: $"Принял, {dbProduct.Name} {dbProduct.Count} шт за {dbProduct.Price} р.",
                cancellationToken: cancellationToken
            );
        }

        else
        {
            // TODO мб if выглядит косячно, но вроде работает :)
            
            if (message.Text == "Нет")
                return;
            log.LogInformation("Cant parse text {text} too product", message.Text);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Если вводишь продукты текстом, нужно что-то такое 🤨🤨🤨" +
                      "\n\nОранжевые апельсины 2 200.22\n\n" +
                      "(Название Количество Итоговая цена)",
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task HandlePhotoAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        if (message.Photo == null)
            return;

        var chatId = message.Chat.Id;
        var userName = message.Chat.Username;

        var fileId = message.Photo.Last().FileId;
        var fileInfo = await client.GetFileAsync(fileId, cancellationToken: cancellationToken);

        if (fileInfo.FilePath == null)
            throw new ArgumentException("FilePath is null");

        var filePath = fileInfo.FilePath;

        log.LogInformation("Received a '{photoPath}' message in chat {chatId}", filePath, chatId);

        var encryptedContent = Array.Empty<byte>();

        if (fileInfo.FileSize != null)
        {
            using var stream = new MemoryStream((int) fileInfo.FileSize.Value);
            await client.DownloadFileAsync(filePath, stream, cancellationToken);
            encryptedContent = stream.ToArray();
        }

        await HandleReceiptAsync(client, chatId, userName, encryptedContent, cancellationToken);
    }

    private async Task HandleReceiptAsync(ITelegramBotClient client, long chatId, string? userName,
        byte[] encryptedContent, CancellationToken cancellationToken)
    {
        var problemText = "Не удалось обработать чек, попробуйте снова";

        try
        {
            log.LogInformation("Send request to receipt api in {chatId}", chatId);

            var receipt = await receiptApiClient.GetReceipt(encryptedContent);
            var products = receipt.Products;

            if (products != null)
            {
                await SendProductsMessagesAsync(client, chatId, userName, products, cancellationToken);
                return;
            }
        }
        catch (ReceiptNotFoundException)
        {
            problemText = "Не удалось обработать чек, возможно, на фото нет чека";
        }
        catch (JsonException)
        {
            problemText = "Обработка изображений временно недоступна";
        }

        log.LogInformation("Received a '{problemText}' message in chat {chatId}", problemText, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: problemText,
            cancellationToken: cancellationToken);
    }

    private async Task SendProductsMessagesAsync(ITelegramBotClient client, long chatId, string? userName,
        IEnumerable<Product> products, CancellationToken cancellationToken)
    {
        var receiptGuid = Guid.NewGuid();

        foreach (var product in products)
        {
            var text = $"{product.Name}";
            var guid = Guid.NewGuid();

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                guid,
                $"{product.TotalPrice} р.",
                $"{product.Count} шт.",
                "🛒");

            // TODO fix it
            // dbDriver.AddProduct(guid, product, receiptGuid, userName, message.Chat.Id);

            log.LogInformation("Send product {ProductId} inline button to chat {ChatId}", guid, chatId);

            await client.SendTextMessageAsync(
                chatId,
                text,
                replyMarkup: inlineKeyboardMarkup,
                disableNotification: true,
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callback,
        CancellationToken cancellationToken)
    {
        if (callback.Message != null && callback.Data != null && Guid.TryParse(callback.Data, out var guid))
        {
            if (callback.Message.ReplyMarkup == null)
                throw new InvalidOperationException();

            var inlineKeyboard = callback.Message.ReplyMarkup.InlineKeyboard.First().ToArray();

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                guid,
                inlineKeyboard[0].Text,
                inlineKeyboard[1].Text,
                inlineKeyboard[2].Text == "🛒" ? "✅" : "🛒");

            if (inlineKeyboard[2].Text == "🛒")
            {
                log.LogInformation("User {UserId} decided to pay for the product {ProductId}", callback.From, guid);

                // TODO fix it
                // var teamId = dbDriver.GetTeamIdByUserTelegramId(callback.From.Username!);
                // dbDriver.AddUserProductBinding(callback.From.Username, teamId, guid);
            }
            else
            {
                log.LogInformation("User {UserId} refused to pay for the product {ProductId}", callback.From, guid);

                // TODO fix it
                // var teamId = dbDriver.GetTeamIdByUserTelegramId(callback.From.Username!);
                // dbDriver.DeleteUserProductBinding(callback.From.Username, teamId, guid);
            }

            await client.EditMessageTextAsync(
                callback.Message.Chat.Id,
                callback.Message.MessageId,
                callback.Message.Text ?? throw new InvalidOperationException(),
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        else
            await client.AnswerCallbackQueryAsync(callback.Id, cancellationToken: cancellationToken);
    }
}