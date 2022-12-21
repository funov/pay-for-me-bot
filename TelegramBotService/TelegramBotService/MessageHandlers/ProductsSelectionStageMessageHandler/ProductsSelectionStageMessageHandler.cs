using AutoMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReceiptApiClient.Exceptions;
using ReceiptApiClient.ReceiptApiClient;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.Models;

namespace TelegramBotService.TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;

public class ProductsSelectionStageMessageHandler : IProductsSelectionStageMessageHandler
{
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

    private readonly ILogger<ProductsSelectionStageMessageHandler> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;

    public ProductsSelectionStageMessageHandler(
        ILogger<ProductsSelectionStageMessageHandler> log,
        IReceiptApiClient receiptApiClient,
        IKeyboardMarkup keyboardMarkup,
        IMapper mapper,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository)
    {
        this.log = log;
        this.receiptApiClient = receiptApiClient;
        this.keyboardMarkup = keyboardMarkup;
        this.mapper = mapper;
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

        var user = userRepository.GetUser(chatId);
        var teamId = user.TeamId;

        switch (message.Text!)
        {
            // TODO Добавить ограничение завершения только на лидера группы
            // TODO Рефакторинг

            case "Перейти к разделению счёта💴":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ты уверен, что все участники команды уже готовы делить счет?" +
                          "\n\n" +
                          "После этого бот подсчитает, кто кому сколько должен и скинет реквизиты для оплаты" +
                          "\n\n" +
                          "Вернуться к вводу/выбору продуктов будет невозможно!",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] { "Да!", "Нет🫣" }),
                    cancellationToken: cancellationToken);
                return;
            case "/help":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: HelpMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                return;
            case "Да!":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Отправь мне свой номер телефона и ссылку на реквизиты в Тинькофф банк (если она есть).",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                userRepository.ChangeUserStage(chatId, teamId, "end");
                return;
            case "Нет🫣":
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Нажми, как будете готовы делить счет!",
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(new[] { "Перейти к разделению счёта💴" }),
                    cancellationToken: cancellationToken);
                return;
        }

        if (Product.TryParse(message.Text!, out var dbProduct))
        {
            var productGuid = Guid.NewGuid();

            log.LogInformation("@{userName} added product {productGuid} in chat {chatId}",
                userName, productGuid, chatId);

            var teamUserChatIds = userRepository
                .GetUserChatIdsByTeamId(teamId)
                .ToList();

            var receiptId = Guid.NewGuid();
            var productId = AddProduct(dbProduct, receiptId, chatId, teamId);

            var products = new List<Product> { dbProduct };
            var productIds = new List<Guid> { productId };

            await SendAddedProductsToTeammatesAsync(teamUserChatIds, userName, client, products, productIds,
                cancellationToken);
        }
        else
        {
            log.LogInformation("Can't parse text from @{userName} {text} to product in chat {chatId}",
                userName, message.Text, chatId);

            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "Если вводишь продукты текстом, нужно что-то такое 🤨🤨🤨" +
                      "\n\n<b>Оранжевые апельсины 2 200.22</b>\n\n" +
                      "(<b>Название Количество Общая цена</b>)",
                parseMode: ParseMode.Html,
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

        log.LogInformation("Received a '{photoPath}' message from @{userName} in chat {chatId}",
            filePath, userName, chatId);

        var encryptedContent = Array.Empty<byte>();

        if (fileInfo.FileSize != null)
        {
            using var stream = new MemoryStream((int)fileInfo.FileSize.Value);
            await client.DownloadFileAsync(filePath, stream, cancellationToken);
            encryptedContent = stream.ToArray();
        }

        await HandleReceiptAsync(client, chatId, userName, encryptedContent, cancellationToken);
    }

    private async Task HandleReceiptAsync(ITelegramBotClient client, long chatId, string? userName,
        byte[] encryptedContent, CancellationToken cancellationToken)
    {
        string problemText;

        try
        {
            log.LogInformation("Send request to receipt api from @{userName} in {chatId}", userName, chatId);

            var receipt = await receiptApiClient.GetReceipt(encryptedContent);

            if (receipt.Products == null)
                return;

            var products = receipt.Products.Select(x => mapper.Map<Product>(x)).ToList();

            var user = userRepository.GetUser(chatId);
            var teamId = user.TeamId;
            var teamUserChatIds = userRepository
                .GetUserChatIdsByTeamId(teamId)
                .ToList();
            var receiptId = Guid.NewGuid();
            var productIds = AddProducts(products, receiptId, chatId, teamId);

            await SendAddedProductsToTeammatesAsync(teamUserChatIds, userName, client, products, productIds,
                cancellationToken);

            return;
        }
        catch (ReceiptNotFoundException)
        {
            problemText = "Не удалось обработать чек, возможно, на фото нет чека";
        }
        catch (JsonException)
        {
            problemText = "Обработка изображений временно недоступна";
        }

        log.LogInformation("Send a '{problemText}' message to @{userName} in chat {chatId}",
            problemText, userName, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: problemText,
            cancellationToken: cancellationToken);
    }

    private async Task SendProductsMessagesAsync(ITelegramBotClient client, long chatId, string? userName,
        IReadOnlyList<Product> products, IReadOnlyList<Guid> productIds, CancellationToken cancellationToken)
    {
        for (var i = 0; i < products.Count; i++)
        {
            var text = $"{products[i].Name}";
            var productId = productIds[i];

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                productId,
                $"{products[i].TotalPrice} р.",
                $"{products[i].Count} шт.",
                "🛒");

            log.LogInformation("Send product {productId} inline button to @{userName} in chat {chatId}",
                productId, userName, chatId);

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
        if (callback.Message != null && callback.Data != null && Guid.TryParse(callback.Data, out var productId))
        {
            if (callback.Message.ReplyMarkup == null)
                throw new InvalidOperationException();

            var inlineKeyboard = callback.Message.ReplyMarkup.InlineKeyboard.First().ToArray();

            var inlineKeyboardMarkup = keyboardMarkup.GetInlineKeyboardMarkup(
                productId,
                inlineKeyboard[0].Text,
                inlineKeyboard[1].Text,
                inlineKeyboard[2].Text == "🛒" ? "✅" : "🛒");

            var chatId = callback.From.Id;
            var userName = callback.From.Username;

            var user = userRepository.GetUser(chatId);
            var teamId = user.TeamId;

            var id = Guid.NewGuid();

            if (inlineKeyboard[2].Text == "🛒")
            {
                log.LogInformation("User @{userName} decided to pay for the product {ProductId} in chat {chatId}",
                    userName, productId, chatId);

                userProductBindingRepository.AddUserProductBinding(id, chatId, teamId, productId);
            }
            else
            {
                log.LogInformation("User @{userName} refused to pay for the product {ProductId} in chat {chatId}",
                    userName, productId, chatId);

                userProductBindingRepository.DeleteUserProductBinding(chatId, teamId, productId);
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

    private List<Guid> AddProducts(IEnumerable<Product> products, Guid receiptId, long chatId, Guid teamId)
    {
        var productsIds = new List<Guid>();
        var sqliteProviderProducts = new List<SqliteProvider.Models.Product>();

        foreach (var product in products)
        {
            var productId = Guid.NewGuid();

            var sqliteProviderProduct = new SqliteProvider.Models.Product
            {
                Id = productId,
                Name = product.Name,
                Count = product.Count,
                Price = product.Price,
                TotalPrice = product.TotalPrice,
                TeamId = teamId,
                ReceiptId = receiptId,
                BuyerChatId = chatId
            };

            productsIds.Add(productId);
            sqliteProviderProducts.Add(sqliteProviderProduct);
        }

        productRepository.AddProducts(sqliteProviderProducts);

        return productsIds;
    }

    private Guid AddProduct(Product product, Guid receiptId, long chatId, Guid teamId)
    {
        var productId = Guid.NewGuid();

        var sqliteProviderProduct = new SqliteProvider.Models.Product
        {
            Id = productId,
            Name = product.Name,
            Count = product.Count,
            Price = product.Price,
            TotalPrice = product.TotalPrice,
            TeamId = teamId,
            ReceiptId = receiptId,
            BuyerChatId = chatId
        };

        productRepository.AddProduct(sqliteProviderProduct);
        return productId;
    }

    private async Task SendProductOwnersUsernameAsync(ITelegramBotClient client, long chatId,
        CancellationToken cancellationToken, string buyerUserName)
    {
        var text = $"@{buyerUserName} добавил продукты!";
        await client.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            cancellationToken: cancellationToken);
    }

    private async Task SendAddedProductsToTeammatesAsync(
        List<long> teamUserChatIds,
        string? userName,
        ITelegramBotClient client,
        IReadOnlyList<Product> products,
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        foreach (var teamUserChatId in teamUserChatIds)
        {
            var user = userRepository.GetUser(teamUserChatId);
            var teamUsername = user.Username;

            if (userName != null && teamUsername != userName)
            {
                await SendProductOwnersUsernameAsync(client, teamUserChatId, cancellationToken, userName);
            }

            await SendProductsMessagesAsync(client, teamUserChatId, teamUsername,
                products, productIds, cancellationToken);
        }
    }
}