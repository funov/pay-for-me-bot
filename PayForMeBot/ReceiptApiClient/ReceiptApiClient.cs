using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.ReceiptApiClient.JsonObjects;
using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.TelegramBotService.Exceptions;

namespace PayForMeBot.ReceiptApiClient;

public class ReceiptApiClient : IReceiptApiClient
{
    private readonly ILogger<ReceiptApiClient> log;
    private readonly IConfiguration config;
    private readonly IMapper mapper;
    private readonly HttpClient httpClient;

    public ReceiptApiClient(ILogger<ReceiptApiClient> log, IConfiguration config, IMapper mapper)
    {
        this.log = log;
        this.config = config;
        this.mapper = mapper;
        httpClient = new HttpClient();
    }

    public async Task<Receipt> GetReceipt(byte[] receiptImageBytes)
    {
        var url = config.GetValue<string>("RECEIPT_API_URL");
        var token = config.GetValue<string>("RECEIPT_API_TOKEN");

        if (url == null)
            throw new NullReceiptApiUrlException("Configuration error");
        if (token == null)
            throw new NullTokenException("Configuration error");

        var formData = new MultipartFormDataContent();
        formData.Add(new ByteArrayContent(receiptImageBytes), "qrfile", "filename");
        formData.Add(new StringContent(token), "token");

        log.LogInformation("Send request to {url}", url);

        var response = await httpClient.PostAsync(url, formData);
        var stringResponse = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        var jObject = JObject.Parse(stringResponse);

        var code = (jObject["code"] ?? throw new JsonException("Receipt api send unexpected json")).Value<int>();

        if (code != 1)
            throw new ReceiptNotFoundException(code);

        var receiptData = jObject.ToObject<ReceiptApiResponse>()?.Data?.Json
                          ?? throw new JsonException("Process json failed");

        return mapper.Map<Receipt>(receiptData);
    }
}