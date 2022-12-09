namespace PayForMeBot.SqliteDriver.Exceptions;

public class NullConnectionStringException : Exception
{
    public NullConnectionStringException(string message) : base(message)
    {
    }
}