using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeEventsCheckout.ServerlessWorker.Services;

namespace StripeEventsCheckout.ServerlessWorker.Workflows;

public class DurableOrchestration
{
    private readonly IStripeClient _stripeClient;
    private readonly SendGridNotifier _sendGridNotifier;
    private readonly TwilioNotifier _twilioNotifier;
    private readonly IConfiguration _configuration;

    public DurableOrchestration(IStripeClient stripeClient, SendGridNotifier sendGridNotifier,
        TwilioNotifier twilioNotifier, IConfiguration configuration)
    {
        _stripeClient = stripeClient;
        _sendGridNotifier = sendGridNotifier;
        _twilioNotifier = twilioNotifier;
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
        //TODO: Email invoice??
    }

    [Function(nameof(CheckoutEvents_SendOrderConfirmation))]
    public async Task CheckoutEvents_SendOrderConfirmation([ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_SendOrderConfirmation));
        
        logger.LogInformation("Retrieving checkout session information");
        var sessionService = new SessionService(_stripeClient);
        var customerSession = await sessionService.GetAsync(checkoutData.CheckoutId);
        //TODO: What happens if we can't find the session?
        
        logger.LogInformation("Sending customer notification");
        const string message = "Thanks for your order! You'll be receiving your ticket(s) soon 🙂";
        await _twilioNotifier.SendMessageAsync(message, customerSession.CustomerDetails.Phone);
    }

    [Function(nameof(CheckoutEvents_UpdateInventory))]
    public async Task CheckoutEvents_UpdateInventory([ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateInventory));
        logger.LogInformation("Updating inventory");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Function(nameof(CheckoutEvents_ScheduleDelivery))]
    public async Task CheckoutEvents_ScheduleDelivery([ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_ScheduleDelivery));
        logger.LogInformation("Scheduling delivery. Generating ticket(s)...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Function(nameof(CheckoutEvents_UpdateCustomer))]
    public async Task CheckoutEvents_UpdateCustomer([ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateCustomer));
        var sessionService = new SessionService(_stripeClient);
        var customerSession = await sessionService.GetAsync(checkoutData.CheckoutId);
        
        logger.LogInformation("Emailing tickets to customer");
        const string message = "Here are your tickets";
        await _sendGridNotifier.SendMessageAsync(message, customerSession.CustomerDetails.Email);
    }
}