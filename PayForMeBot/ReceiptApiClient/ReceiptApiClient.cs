using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PayForMeBot.ReceiptApiClient.Exceptions;
using PayForMeBot.ReceiptApiClient.JsonObjects;
using PayForMeBot.ReceiptApiClient.Models;

namespace PayForMeBot.ReceiptApiClient;

public class ReceiptApiClient : IReceiptApiClient
{
    private readonly ILogger<ReceiptApiClient> log;
    private readonly IMapper mapper;
    private readonly ReceiptApiService receiptApiService;

    public ReceiptApiClient(ILogger<ReceiptApiClient> log, IMapper mapper, ReceiptApiService receiptApiService)
    {
        this.log = log;
        this.mapper = mapper;
        this.receiptApiService = receiptApiService;
    }

    public async Task<Receipt> GetReceipt(byte[] receiptImageBytes)
    {
        var jObject = await receiptApiService.GetReceiptApiResult(receiptImageBytes);

        var code = (jObject["code"] ?? throw new JsonException("Receipt api send unexpected json")).Value<int>();

        if (code != 1)
            throw new ReceiptNotFoundException(code);

        var receiptData = jObject.ToObject<ReceiptApiResponse>()?.Data?.Json
                          ?? throw new JsonException("Process json failed");

        return mapper.Map<Receipt>(receiptData);
    }
}