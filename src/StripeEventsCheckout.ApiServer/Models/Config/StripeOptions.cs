namespace StripeEventsCheckout.ApiServer.Models.Config;
public class StripeOptions
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PubSubName { get; set; } = string.Empty;
}