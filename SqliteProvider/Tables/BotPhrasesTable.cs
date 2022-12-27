using System.ComponentModel.DataAnnotations;
using SqliteProvider.Types;

namespace SqliteProvider.Tables;

public class BotPhrasesTable
{
    [Key] 
    public BotPhrase Type { get; set; }
    public string? Phrase { get; set; }
}