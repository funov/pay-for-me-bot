using ReceiptApiClient.Models;

namespace ReceiptApiClient;

public interface IReceiptApiClient
{
    public Task<Receipt> GetReceipt(byte[] receiptImageBytes);
}