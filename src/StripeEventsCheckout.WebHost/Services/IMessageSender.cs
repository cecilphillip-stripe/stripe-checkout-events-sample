namespace StripeEventsCheckout.WebHost.Services;

public interface IMessageSender
{
    Task SendMessageAsync(string message, string receiver);
}