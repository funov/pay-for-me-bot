namespace PayForMeBot.ReceiptApiClient.Exceptions;

public class NullTelegramTokenException : Exception
{
    public NullTelegramTokenException(string message) : base(message)
    {
    }
}