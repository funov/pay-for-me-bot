using System.Text.Json;
using AutoMapper;
using Newtonsoft.Json.Linq;
using ReceiptApiClient.Exceptions;
using ReceiptApiClient.JsonObjects;
using ReceiptApiClient.Models;

namespace ReceiptApiClient.ReceiptApiClient;

public class ReceiptApiClient : IReceiptApiClient
{
    private readonly IMapper mapper;
    private readonly ReceiptApiService receiptApiService;

    public ReceiptApiClient(IMapper mapper, ReceiptApiService receiptApiService)
    {
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