using Newtonsoft.Json;

namespace ReceiptApiClient.JsonObjects;

[JsonObject]
public class ReceiptData
{
    [JsonProperty("items")] 
    public Item[]? Items { get; set; }

    [JsonProperty("retailPlace")] 
    public string? ShopName { get; set; }

    [JsonProperty("totalSum")] 
    public int TotalSum { get; set; }
}