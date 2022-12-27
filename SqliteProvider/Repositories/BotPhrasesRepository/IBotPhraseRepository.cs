using SqliteProvider.Types;

namespace SqliteProvider.Repositories.BotPhrasesRepository;

public interface IBotPhraseRepository
{
    string? GetBotPhrase(BotPhraseType phraseType);
}