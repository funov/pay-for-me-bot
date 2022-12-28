namespace SqliteProvider.Exceptions;

public class EmptyBotPhrasesException : Exception
{
    public EmptyBotPhrasesException(string message) : base(message)
    {
    }
}