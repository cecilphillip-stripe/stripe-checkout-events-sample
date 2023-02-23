namespace StripeEventsCheckout.ServerlessWorker.Services;

public interface INotifier
{
    Task SendMessageAsync(string message, string receiver);
}