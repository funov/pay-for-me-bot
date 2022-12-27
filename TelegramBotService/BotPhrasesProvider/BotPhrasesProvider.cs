namespace TelegramBotService.BotPhrasesProvider;

public class BotPhrasesProvider : IBotPhrasesProvider
{
    public string Help { get; }

    public BotPhrasesProvider(IBotPhrasesProvider botPhrasesProvider)
    {
        Help = botPhrasesProvider.Help;
    }
}