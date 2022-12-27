using System.ComponentModel.DataAnnotations;
using SqliteProvider.Types;

namespace SqliteProvider.Tables;

public class BotPhrasesTable
{
    [Key] 
    public BotPhraseType Type { get; set; }
    public string? Phrase { get; set; }
}