using Microsoft.Extensions.Configuration;
using SqliteProvider.Types;

namespace SqliteProvider.Repositories.BotPhrasesRepository;

public class BotPhraseRepository : IBotPhraseRepository
{
    private readonly DbContext db;

    public BotPhraseRepository(IConfiguration config)
        => db = new DbContext(config.GetValue<string>("DbConnectionString"));

    public string? GetBotPhrase(BotPhraseType phraseType)
        => db.BotPhrases.FirstOrDefault(botPhrasesTable => botPhrasesTable.Type == phraseType)?.Phrase;
}