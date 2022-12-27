using SqliteProvider.Repositories.BotPhrasesRepository;
using SqliteProvider.Types;

namespace TelegramBotService.BotPhrasesProvider;

public class BotPhrasesProvider : IBotPhrasesProvider
{
    public string? Help { get; }

    public BotPhrasesProvider(IBotPhraseRepository botPhraseRepository)
    {
        Help = botPhraseRepository.GetBotPhrase(BotPhrase.Help);
    }
}