using PayForMeBot.ReceiptApiClient.Models;

namespace PayForMeBot.ReceiptApiClient;

public interface IReceiptApiClient
{
    public Task<Receipt> GetReceipt(byte[] receiptImageBytes);
}