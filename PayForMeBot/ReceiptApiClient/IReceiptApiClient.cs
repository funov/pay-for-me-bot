using PayForMeBot.ReceiptApiClient.JsonObjects;

namespace PayForMeBot.ReceiptApiClient;

public interface IReceiptApiClient
{
    public Task<ReceiptApiResponse> SendReceiptImage(byte[] imageBytes);
}