namespace StripeEventsCheckout.ApiServer.Models.Config;

public class TwilioOptions
{
    public string AccountSID { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
