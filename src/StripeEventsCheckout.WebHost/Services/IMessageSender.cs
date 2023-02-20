namespace StripeEventsCheckout.WebHost.Services;

public interface IMessageSender
{
    Task SendMessageAsync(string message, string receiver, string contentType, IDictionary<string,string> metadata = null);
}