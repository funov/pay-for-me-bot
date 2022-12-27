using DebtsCalculator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.TelegramBotService;
using ReceiptApiClient;
using ReceiptApiClient.ReceiptApiClient;
using Serilog;
using SqliteProvider.Repositories.BotPhrasesRepository;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;
using TelegramBotService.BotPhrasesProvider;
using TelegramBotService.MessageHandlers.PaymentStageMessageHandler;
using TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;
using TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;
using TelegramBotService.ProductInlineButtonSender;

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

                services.AddSingleton<IDebtsCalculator, DebtsCalculator.DebtsCalculator>();

                services.AddSingleton<ITelegramBotService, TelegramBotService.TelegramBotService>();
                services.AddSingleton<IKeyboardMarkup, KeyboardMarkup.KeyboardMarkup>();
                services
                    .AddSingleton<IProductInlineButtonSender, ProductInlineButtonSender.ProductInlineButtonSender>();
                services.AddSingleton<IBotPhrasesProvider, BotPhrasesProvider.BotPhrasesProvider>();

                services.AddSingleton<IPaymentStageMessageHandler, PaymentStageMessageHandler>();
                services.AddSingleton<IProductsSelectionStageMessageHandler, ProductsSelectionStageMessageHandler>();
                services.AddSingleton<ITeamAdditionStageMessageHandler, TeamAdditionStageMessageHandler>();

                services.AddSingleton<IProductRepository, ProductRepository>();
                services.AddSingleton<IUserRepository, UserRepository>();
                services.AddSingleton<IUserProductBindingRepository, UserProductBindingRepository>();
                services.AddSingleton<IBotPhraseRepository, BotPhraseRepository>();

                services.AddAutoMapper(typeof(ReceiptApiClient.ReceiptApiClient.ReceiptApiClient).Assembly);
                services.AddAutoMapper(typeof(ProductsSelectionStageMessageHandler).Assembly);
                services.AddAutoMapper(typeof(ProductRepository).Assembly);
                services.AddAutoMapper(typeof(UserRepository).Assembly);
                services.AddAutoMapper(typeof(UserProductBindingRepository).Assembly);
            })
            .UseSerilog()
            .Build();

        var service = ActivatorUtilities.CreateInstance<TelegramBotService.TelegramBotService>(host.Services);
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