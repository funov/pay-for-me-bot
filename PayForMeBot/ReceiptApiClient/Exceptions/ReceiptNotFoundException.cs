namespace PayForMeBot.ReceiptApiClient.Exceptions;

public class ReceiptNotFoundException : Exception
{
    public int Code { get; }
    
    public ReceiptNotFoundException(int code) : base()
    {
        Code = code;
    }
}