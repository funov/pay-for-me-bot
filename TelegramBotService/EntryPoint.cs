using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayForMeBot.TelegramBotService;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using PayForMeBot.TelegramBotService.MessageHandler.EndStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.MiddleStageMessageHandler;
using PayForMeBot.TelegramBotService.MessageHandler.StartStageMessageHandler;
using ReceiptApiClient;
using ReceiptApiClient.ReceiptApiClient;
using Serilog;
using SqliteProvider.SqliteProvider;

namespace PayForMeBot;

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
                services.AddSingleton<IEndStageMessageHandler, EndStageMessageHandler>();
                services.AddSingleton<IMiddleStageMessageHandler, MiddleStageMessageHandler>();
                services.AddSingleton<IStartStageMessageHandler, StartStageMessageHandler>();
                services.AddSingleton<IKeyboardMarkup, KeyboardMarkup>();
                services.AddSingleton<ISqliteProvider, SqliteProvider.SqliteProvider.SqliteProvider>();
                services.AddAutoMapper(typeof(ReceiptApiClient.ReceiptApiClient.ReceiptApiClient).Assembly);
                services.AddAutoMapper(typeof(MiddleStageMessageHandler).Assembly);
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