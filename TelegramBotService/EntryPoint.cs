using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentLogic.DebtsCalculator;
using PaymentLogic.RequisiteParser.BankingLinkVerifier;
using PaymentLogic.RequisiteParser.BankingLinkVerifier.Implementations;
using PaymentLogic.RequisiteParser.PhoneNumberVerifier;
using PaymentLogic.RequisiteParser.PhoneNumberVerifier.Implementations;
using PaymentLogic.RequisiteParser.RequisiteMessageParser;
using TelegramBotService.TelegramBotService;
using ReceiptApiClient;
using ReceiptApiClient.ReceiptApiClient;
using Serilog;
using SqliteProvider.Exceptions;
using SqliteProvider.Repositories.BotPhrasesRepository;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using SqliteProvider.Transactions.DeleteAllTeamIdTransaction;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.ButtonUtils.KeyboardMarkup;
using TelegramBotService.ButtonUtils.ProductInlineButtonSender;
using TelegramBotService.MessageHandlers.PaymentStageMessageHandler;
using TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;
using TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

namespace TelegramBotService;

public static class Program
{
    public static async Task Main()
    {
        var builder = new ConfigurationBuilder();
        BuildConfig(builder);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Build())
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        Log.Logger.Information("Application Starting");

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IReceiptApiClient, ReceiptApiClient.ReceiptApiClient.ReceiptApiClient>();
                services.AddHttpClient<ReceiptApiService>();

                services.AddSingleton<IDebtsCalculator, DebtsCalculator>();
                services.AddSingleton<PhoneNumberVerifier, RussianPhoneNumberVerifier>();
                services.AddSingleton<BankingLinkVerifier, TinkoffLinkVerifier>();
                services.AddSingleton<IRequisiteMessageParser, RequisiteMessageParser>();

                services.AddSingleton<ITelegramBotService, TelegramBotService.TelegramBotService>();
                services.AddSingleton<IKeyboardMarkup, KeyboardMarkup>();
                services
                    .AddSingleton<IProductInlineButtonSender, ProductInlineButtonSender>();
                services.AddSingleton<IBotPhrasesProvider, BotPhrasesProvider.BotPhrasesProvider>();

                services.AddSingleton<IPaymentStageMessageHandler, PaymentStageMessageHandler>();
                services.AddSingleton<IProductsSelectionStageMessageHandler, ProductsSelectionStageMessageHandler>();
                services.AddSingleton<ITeamAdditionStageMessageHandler, TeamAdditionStageMessageHandler>();

                services.AddSingleton<IProductRepository, ProductRepository>();
                services.AddSingleton<IUserRepository, UserRepository>();
                services.AddSingleton<IUserProductBindingRepository, UserProductBindingRepository>();
                services.AddSingleton<IBotPhraseRepository, BotPhraseRepository>();
                services.AddSingleton<IDeleteAllTeamIdTransaction, DeleteAllTeamIdTransaction>();

                services.AddAutoMapper(typeof(ReceiptApiClient.ReceiptApiClient.ReceiptApiClient).Assembly);
                services.AddAutoMapper(typeof(ProductsSelectionStageMessageHandler).Assembly);
                services.AddAutoMapper(typeof(ProductRepository).Assembly);
                services.AddAutoMapper(typeof(UserRepository).Assembly);
                services.AddAutoMapper(typeof(UserProductBindingRepository).Assembly);
            })
            .UseSerilog()
            .Build();

        TelegramBotService.TelegramBotService service;

        try
        {
            service = ActivatorUtilities.CreateInstance<TelegramBotService.TelegramBotService>(host.Services);
        }
        catch (EmptyBotPhrasesException)
        {
            Log.Logger.Error("Something went wrong with BotPhrases table in db");
            return;
        }

        await service.Run();
    }

    private static void BuildConfig(IConfigurationBuilder builder)
    {
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
    }
}