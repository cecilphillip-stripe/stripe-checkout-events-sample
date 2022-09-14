using Stripe;
using StripeEventsCheckout.IdentityServer.Data.MongoData;

namespace StripeEventsCheckout.IdentityServer;

public class ChannelWorker: BackgroundService
{
    private readonly IStripeClient _stripeClient;
    private readonly ICustomerDataStore _dataStore;
    private readonly ChannelNotifier _notifier;
    private readonly ILogger<ChannelWorker> _logger;

    public ChannelWorker(IStripeClient stripeClient, ICustomerDataStore dataStore, ChannelNotifier notifier, ILogger<ChannelWorker> logger)
    {
        _stripeClient = stripeClient;
        _dataStore = dataStore;
        _notifier = notifier;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var customerService = new CustomerService(_stripeClient);
        while (await _notifier.CreateStripeAccountReader.WaitToReadAsync(stoppingToken))
        {
            // Create customer associated customer object in Stripe
            if (_notifier.CreateStripeAccountReader.TryRead(out var item))
            {
                var createOptions = new CustomerCreateOptions
                {
                    Email = item.Email,
                    Phone = item.PhoneNumber,
                    Name = $"{item.FirstName} {item.LastName}",
                    Metadata = new()
                    {
                        ["user_id"] = item.CustomerId,
                        ["app_source"] = nameof(StripeEventsCheckout)
                    }
                };
                var createdStripeCustomer = await customerService.CreateAsync(createOptions, cancellationToken: stoppingToken);
                _ = await _dataStore.SetStripeCustomerInfo(item.CustomerId, createdStripeCustomer.Id);
            }
        }
    }
}