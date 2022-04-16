using System.Text.Json.Serialization;

namespace StripeEventsCheckout.BlazorUI.Models;

public class EventInfo
{
    [JsonPropertyName("price_id")]
    public string StripePriceId { get; set; } = string.Empty;

    [JsonPropertyName("product_id")]
    public string StripeProductId { get; set; }= string.Empty;

    [JsonPropertyName("owner")]
    public string Owner { get; set; }= string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; }= string.Empty;

    [JsonPropertyName("unit_amount")]
    public long Amount { get; set; }

    [JsonPropertyName("images")]
    public string[] Images { get; set; } = Array.Empty<string>();
}