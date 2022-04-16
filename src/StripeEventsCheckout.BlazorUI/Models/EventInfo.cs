using System.Text.Json;
using System.Text.Json.Serialization;

namespace StripeEventsCheckout.BlazorUI.Models;

public class EventInfo
{
    [JsonPropertyName("price_id")]
    public string StripePriceId { get; set; }

    [JsonPropertyName("product_id")]
    public string StripeProductId { get; set; }

    [JsonPropertyName("owner")]
    public string Owner { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("unit_amount")]
    public long Amount { get; set; }

    [JsonPropertyName("images")]
    public string[] Images { get; set; }
}