using Newtonsoft.Json;

namespace ReceiptApiClient.JsonObjects;

[JsonObject]
public class ResponseData
{
    [JsonProperty("json")] 
    public ReceiptData? Json { get; set; }
}