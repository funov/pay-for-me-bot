using AutoMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReceiptApiClient.Exceptions;
using ReceiptApiClient.ReceiptApiClient;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Types;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.ButtonUtils.KeyboardMarkup;
using TelegramBotService.ButtonUtils.ProductInlineButtonSender;
using TelegramBotService.Models;

namespace TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;

public class ProductsSelectionStageMessageHandler : IProductsSelectionStageMessageHandler
{
    private readonly ILogger<ProductsSelectionStageMessageHandler> log;
    private readonly IReceiptApiClient receiptApiClient;
    private readonly IKeyboardMarkup keyboardMarkup;
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;
    private readonly IBotPhrasesProvider botPhrasesProvider;
    private readonly IProductInlineButtonSender productInlineButtonSender;

    private readonly string[] goToSplitPurchasesButtons;
    private readonly string[] goToSplitPurchasesWithQuitButtons;
    private readonly string[] transitionToEndButtons;

    public ProductsSelectionStageMessageHandler(
        ILogger<ProductsSelectionStageMessageHandler> log,
        IReceiptApiClient receiptApiClient,
        IKeyboardMarkup keyboardMarkup,
        IMapper mapper,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository,
        IBotPhrasesProvider botPhrasesProvider,
        IProductInlineButtonSender productInlineButtonSender)
    {
        this.log = log;
        this.receiptApiClient = receiptApiClient;
        this.keyboardMarkup = keyboardMarkup;
        this.mapper = mapper;
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;
        this.botPhrasesProvider = botPhrasesProvider;
        this.productInlineButtonSender = productInlineButtonSender;

        transitionToEndButtons = new[]
            { botPhrasesProvider.TransitionToEndYes, botPhrasesProvider.TransitionToEndNo };
        goToSplitPurchasesWithQuitButtons =
            new[] { botPhrasesProvider.GoToSplitPurchases, botPhrasesProvider.QuitTeam };
        goToSplitPurchasesButtons = new[] { botPhrasesProvider.GoToSplitPurchases };
    }

    public async Task HandleTextAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var username = message.Chat.Username!;

        log.LogInformation("Received a '{messageText}' message in chat {chatId} from @{userName}",
            message.Text, chatId, username);

        var user = userRepository.GetUser(chatId);
        var teamId = user!.TeamId;

        if (message.Text! == botPhrasesProvider.GoToSplitPurchases)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.TransitionToEnd,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(transitionToEndButtons),
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Text! == botPhrasesProvider.TransitionToEndYes)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.SendMeRequisites,
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            userRepository.ChangeUserStage(chatId, teamId, UserStage.Payment);
            return;
        }

        if (message.Text! == botPhrasesProvider.TransitionToEndNo)
        {
            if (productRepository.GetAddedProductsCount(chatId, teamId) == 0)
            {
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.PushIfReadyToSplitPurchase,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesWithQuitButtons),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.PushIfReadyToSplitPurchase,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                    cancellationToken: cancellationToken);
            }

            return;
        }

        if (message.Text! == "/help")
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.Help,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Text! == botPhrasesProvider.QuitTeam)
        {
            // TODO
        }

        if (Product.TryParse(message.Text!, out var dbProduct))
        {
            await HandleTextProductAsync(client, chatId, username, teamId, dbProduct, cancellationToken);
            return;
        }

        log.LogInformation("Can't parse text from @{userName} {text} to product in chat {chatId}",
            username, message.Text, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: botPhrasesProvider.ExampleTextProductInput,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleTextProductAsync(
        ITelegramBotClient client,
        long chatId,
        string username,
        Guid teamId,
        Product dbProduct,
        CancellationToken cancellationToken)
    {
        var productGuid = Guid.NewGuid();

        log.LogInformation("@{userName} added product {productGuid} in chat {chatId}",
            username, productGuid, chatId);

        var teamUserChatIds = userRepository
            .GetUserChatIdsByTeamId(teamId)
            .ToList();

        var receiptId = Guid.NewGuid();

        if (productRepository.GetAddedProductsCount(chatId, teamId) == 0)
        {
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: botPhrasesProvider.AddFirstProduct,
                parseMode: ParseMode.Html,
                replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                cancellationToken: cancellationToken);
        }

        var productId = AddProduct(dbProduct, receiptId, chatId, teamId);

        var products = new List<Product> { dbProduct };
        var productIds = new List<Guid> { productId };

        await SendAddedProductsToTeammatesAsync(teamUserChatIds, username, client, products, productIds,
            cancellationToken);
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

            var receipt = await receiptApiClient.GetReceiptAsync(encryptedContent);

            if (receipt.Products == null)
                return;

            var products = receipt.Products.Select(x => mapper.Map<Product>(x)).ToList();

            var user = userRepository.GetUser(chatId);
            var teamId = user!.TeamId;
            var teamUserChatIds = userRepository
                .GetUserChatIdsByTeamId(teamId)
                .ToList();
            var receiptId = Guid.NewGuid();

            if (productRepository.GetAddedProductsCount(chatId, teamId) == 0)
            {
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: botPhrasesProvider.AddFirstProduct,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboardMarkup.GetReplyKeyboardMarkup(goToSplitPurchasesButtons),
                    cancellationToken: cancellationToken);
            }

            var productIds = AddProducts(products, receiptId, chatId, teamId);

            await SendAddedProductsToTeammatesAsync(teamUserChatIds, userName, client, products, productIds,
                cancellationToken);

            return;
        }
        catch (ReceiptNotFoundException)
        {
            problemText = botPhrasesProvider.ReceiptError;
        }
        catch (JsonException)
        {
            problemText = botPhrasesProvider.ReceiptApiError;
        }

        log.LogInformation("Send a '{problemText}' message to @{userName} in chat {chatId}",
            problemText, userName, chatId);

        await client.SendTextMessageAsync(
            chatId: chatId,
            text: problemText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient client, CallbackQuery callback,
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
            var teamId = user!.TeamId;

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
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: cancellationToken);
            return;
        }

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

    private async Task SendAddedProductsToTeammatesAsync(
        List<long> teamUserChatIds,
        string? buyerName,
        ITelegramBotClient client,
        IReadOnlyList<Product> products,
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        foreach (var teamUserChatId in teamUserChatIds)
        {
            var user = userRepository.GetUser(teamUserChatId);
            var teamUserName = user!.Username!;

            if (buyerName != null && teamUserName != buyerName)
            {
                await client.SendTextMessageAsync(
                    chatId: teamUserChatId,
                    text: $"@{buyerName} добавил продукты!",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }

            await productInlineButtonSender.SendProductsInlineButtonsAsync(client, teamUserChatId, teamUserName,
                products, productIds, cancellationToken);
        }
    }
}