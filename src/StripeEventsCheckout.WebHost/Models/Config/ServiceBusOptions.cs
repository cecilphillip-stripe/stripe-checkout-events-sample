namespace StripeEventsCheckout.WebHost.Models.Config;

public class ServiceBusOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string CheckoutEntityName { get; set; } = string.Empty;
}