namespace PayForMeBot.ReceiptApiClient.Exceptions;

public class ReceiptNotFoundException : Exception
{
    public int Code { get; }

    public ReceiptNotFoundException(int code)
    {
        Code = code;
    }
}