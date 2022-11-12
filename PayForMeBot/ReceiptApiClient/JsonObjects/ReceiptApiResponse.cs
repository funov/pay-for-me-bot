using Newtonsoft.Json;

namespace PayForMeBot.ReceiptApiClient.JsonObjects;

[JsonObject]
public class ReceiptApiResponse
{
    [JsonProperty("code")] 
    public int Code { get; set; }

    [JsonProperty("data")] 
    public ResponseData? Data { get; set; }
}