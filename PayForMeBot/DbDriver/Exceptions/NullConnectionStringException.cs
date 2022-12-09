namespace PayForMeBot.DbDriver.Exceptions;

public class NullConnectionStringException : Exception
{
    public NullConnectionStringException(string message) : base(message)
    {
    }
}