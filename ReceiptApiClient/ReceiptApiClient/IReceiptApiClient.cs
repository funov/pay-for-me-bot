using ReceiptApiClient.Models;

namespace ReceiptApiClient.ReceiptApiClient;

public interface IReceiptApiClient
{
    public Task<Receipt> GetReceiptAsync(byte[] receiptImageBytes);
}