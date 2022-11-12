using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PayForMeBot.ReceiptApiClient.JsonObjects;

namespace PayForMeBot.ReceiptApiClient;

public class ReceiptApiClient : IReceiptApiClient
{
    private readonly ILogger<ReceiptApiClient> log;
    private readonly IConfiguration config;
    private readonly HttpClient httpClient;

    public ReceiptApiClient(ILogger<ReceiptApiClient> log, IConfiguration config)
    {
        this.log = log;
        this.config = config;
        httpClient = new HttpClient();
    }

    public async Task<ReceiptApiResponse> SendReceiptImage(byte[] imageBytes)
    {
        var url = config.GetValue<string>("RECEIPT_API_URL");
        var token = config.GetValue<string>("RECEIPT_API_TOKEN");

        if (url == null || token == null)
        {
            throw new ArgumentException("Url or token don't exists in appsettings.json");
        }

        var formData = new MultipartFormDataContent();
        formData.Add(new ByteArrayContent(imageBytes), "qrfile", "filename");
        formData.Add(new StringContent(token), "token");
        
        log.LogInformation("Send request to {url}", url);
        
        var response = await httpClient.PostAsync(url, formData);
        var stringResponse = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        var jObject = JObject.Parse(stringResponse);
        var receiptApiResponse = jObject.ToObject<ReceiptApiResponse>();

        return receiptApiResponse ?? throw new ArgumentException("Receipt api response is null");
    }
}