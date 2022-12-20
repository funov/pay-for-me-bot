namespace ReceiptApiClient.Exceptions;

public class NullReceiptApiTokenException : Exception
{
    public NullReceiptApiTokenException(string message) : base(message)
    {
    }
}