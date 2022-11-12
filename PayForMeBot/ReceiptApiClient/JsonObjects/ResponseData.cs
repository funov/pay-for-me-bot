using Newtonsoft.Json;

namespace PayForMeBot.ReceiptApiClient.JsonObjects;

[JsonObject]
public class ResponseData
{
    [JsonProperty("json")] 
    public ReceiptData? Json { get; set; }
}