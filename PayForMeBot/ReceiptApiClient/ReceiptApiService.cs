using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.TelegramBotService.Exceptions;

namespace PayForMeBot.ReceiptApiClient;

public class ReceiptApiService
{
    private readonly HttpClient httpClient;
    private readonly string token;

    public ReceiptApiService(HttpClient httpClient, IConfiguration config)
    {
        this.httpClient = httpClient;

        var url = config.GetValue<string>("RECEIPT_API_URL")
                  ?? throw new NullReceiptApiUrlException("Configuration error");

        httpClient.BaseAddress = new Uri(url);

        token = config.GetValue<string>("RECEIPT_API_TOKEN")
                ?? throw new NullTokenException("Configuration error");
    }

    public async Task<JObject> GetReceiptApiResult(byte[] receiptImageBytes)
    {
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(token), "token");
        formData.Add(new ByteArrayContent(receiptImageBytes), "qrfile", "filename");

        var response = await httpClient.PostAsync(string.Empty, formData);
        var stringResponse = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        return JObject.Parse(stringResponse);
    }
}