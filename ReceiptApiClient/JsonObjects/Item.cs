using Newtonsoft.Json;

namespace ReceiptApiClient.JsonObjects;

[JsonObject]
public class Item
{
    [JsonProperty("sum")] 
    public int TotalPrice { get; set; }

    [JsonProperty("price")] 
    public int Price { get; set; }

    [JsonProperty("quantity")] 
    public int Count { get; set; }

    [JsonProperty("name")] 
    public string? Name { get; set; }
}