using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace StripeEventsCheckout.ServerlessWorker;

public class CheckoutEventsProcessor
{
    private readonly ILogger<CheckoutEventsProcessor> _logger;

    public CheckoutEventsProcessor(ILogger<CheckoutEventsProcessor> logger)
    {
        _logger = logger;
    }
    
    [Function("CheckoutEventsProcessor")]
    public void Run(
        [ServiceBusTrigger("stripe-checkout-events", Connection = "ServicebusConnection")] string myQueueItem,
        FunctionContext context)
    {
        _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
    }
}