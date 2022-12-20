namespace ReceiptApiClient.Exceptions;

public class NullReceiptApiUrlException : Exception
{
    public NullReceiptApiUrlException(string message) : base(message)
    {
    }
}