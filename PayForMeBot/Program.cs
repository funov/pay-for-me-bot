using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayForMeBot.ReceiptApiClient;
using PayForMeBot.TelegramBotService;
using PayForMeBot.TelegramBotService.KeyboardMarkup;
using PayForMeBot.TelegramBotService.MessageHandler;
using PayForMeBot.SqliteDriver;
using Serilog;

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
                services.AddSingleton<IReceiptApiClient, ReceiptApiClient.ReceiptApiClient>();
                services.AddSingleton<ITelegramBotService, TelegramBotService.TelegramBotService>();
                services.AddSingleton<IMessageHandler, MessageHandler>();
                services.AddSingleton<IKeyboardMarkup, KeyboardMarkup>();
                services.AddSingleton<ISqliteDriver, SqliteDriver.SqliteDriver>();
                services.AddAutoMapper(typeof(Program).Assembly);
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
            .AddEnvironmentVariables();
    }
}