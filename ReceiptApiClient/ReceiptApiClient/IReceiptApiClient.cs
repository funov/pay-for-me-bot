using ReceiptApiClient.Models;

namespace ReceiptApiClient.ReceiptApiClient;

public interface IReceiptApiClient
{
    public Task<Receipt> GetReceipt(byte[] receiptImageBytes);
}