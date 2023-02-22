using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StripeEventsCheckout.ServerlessWorker;

public class DurableOrchestration
{
    private readonly IConfiguration _configuration;

    public DurableOrchestration(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [Function(nameof(CheckoutEvents_FulfillmentOrchestration))]
    public async Task CheckoutEvents_FulfillmentOrchestration([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var checkoutData = context.GetInput<CheckoutEventPayload>();
        await context.CallActivityAsync("CheckoutEvents_SendOrderConfirmation", checkoutData);
        await context.CallActivityAsync("CheckoutEvents_UpdateInventory", checkoutData);
        await context.CallActivityAsync("CheckoutEvents_ScheduleDelivery", checkoutData);
        await context.CallActivityAsync("CheckoutEvents_UpdateCustomer", checkoutData);
    }
    
    [Function(nameof(CheckoutEvents_SendOrderConfirmation))]
    public async Task CheckoutEvents_SendOrderConfirmation([ActivityTrigger] CheckoutEventPayload checkoutData, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(CheckoutEvents_SendOrderConfirmation));
    }
    
    [Function(nameof(CheckoutEvents_UpdateInventory))]
    public async Task CheckoutEvents_UpdateInventory([ActivityTrigger] CheckoutEventPayload checkoutData, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateInventory));
    }
    
    [Function(nameof(CheckoutEvents_ScheduleDelivery))]
    public async Task CheckoutEvents_ScheduleDelivery([ActivityTrigger] CheckoutEventPayload checkoutData, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(CheckoutEvents_ScheduleDelivery));
    }
    
    [Function(nameof(CheckoutEvents_UpdateCustomer))]
    public async Task CheckoutEvents_UpdateCustomer([ActivityTrigger] CheckoutEventPayload checkoutData, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateCustomer));
    }
}