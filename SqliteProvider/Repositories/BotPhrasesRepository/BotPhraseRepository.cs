using SqliteProvider.Exceptions;
using SqliteProvider.Types;

namespace SqliteProvider.Repositories.BotPhrasesRepository;

public class BotPhraseRepository : IBotPhraseRepository
{
    private readonly DbContext dbContext;

    public BotPhraseRepository(DbContext dbContext)
        => this.dbContext = dbContext;

    public string GetBotPhrase(BotPhraseType phraseType)
        => dbContext.BotPhrases.FirstOrDefault(botPhrasesTable => botPhrasesTable.Type == phraseType)?.Phrase
           ?? throw new EmptyBotPhrasesException("Check BotPhrases table in db");
}