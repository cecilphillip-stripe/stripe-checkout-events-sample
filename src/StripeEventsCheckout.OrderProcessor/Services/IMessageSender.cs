namespace StripeEventsCheckout.OrderProcessor.Services;

public interface IMessageSender
{
    Task SendMessageAsync(string message, string receiver);
}