using System.ComponentModel.DataAnnotations;

namespace SqliteProvider.Tables;

public class BotPhrasesTable
{
    [Key] 
    public string? Name { get; set; }
    public string? Phrase { get; set; }
}