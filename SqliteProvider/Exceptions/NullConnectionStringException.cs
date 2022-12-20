namespace SqliteProvider.Exceptions;

public class NullConnectionStringException : Exception
{
    public NullConnectionStringException(string message) : base(message)
    {
    }
}