namespace StripeEventsCheckout.ApiServer.Services;

public interface IMessageSender
{
    Task SendMessageAsync(string message, string receiver);
}