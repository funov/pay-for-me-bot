using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramBotService.KeyboardMarkup;
using TelegramBotService.TelegramBotService;
using ReceiptApiClient;
using ReceiptApiClient.ReceiptApiClient;
using SqliteProvider.SqliteProvider;
using Serilog;
using TelegramBotService.TelegramBotService.MessageHandlers.PaymentStageMessageHandler;
using TelegramBotService.TelegramBotService.MessageHandlers.ProductsSelectionStageMessageHandler;
using TelegramBotService.TelegramBotService.MessageHandlers.TeamAdditionStageMessageHandler;

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
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IReceiptApiClient, ReceiptApiClient.ReceiptApiClient.ReceiptApiClient>();
                services.AddSingleton<ITelegramBotService, TelegramBotService.TelegramBotService>();
                services.AddSingleton<IPaymentStageMessageHandler, PaymentStageMessageHandler>();
                services.AddSingleton<IProductsSelectionStageMessageHandler, ProductsSelectionStageMessageHandler>();
                services.AddSingleton<ITeamAdditionStageMessageHandler, TeamAdditionStageMessageHandler>();
                services.AddSingleton<IKeyboardMarkup, KeyboardMarkup.KeyboardMarkup>();
                services.AddSingleton<ISqliteProvider, SqliteProvider.SqliteProvider.SqliteProvider>();
                services.AddAutoMapper(typeof(ReceiptApiClient.ReceiptApiClient.ReceiptApiClient).Assembly);
                services.AddAutoMapper(typeof(ProductsSelectionStageMessageHandler).Assembly);
                services.AddAutoMapper(typeof(SqliteProvider.SqliteProvider.SqliteProvider).Assembly);
                services.AddHttpClient<ReceiptApiService>();
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