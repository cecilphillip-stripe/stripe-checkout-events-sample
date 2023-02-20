using System;
using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace StripeEventsCheckout.ServerlessWorker;

public class CheckoutEventsProcessor
{
    private readonly IValidator<CheckoutEventPayload> _checkoutEventValidator;
    private readonly ILogger<CheckoutEventsProcessor> _logger;

    public CheckoutEventsProcessor(IValidator<CheckoutEventPayload> checkoutEventValidator, ILogger<CheckoutEventsProcessor> logger)
    {
        _checkoutEventValidator = checkoutEventValidator;
        _logger = logger;
    }

    [Function("CheckoutEventsProcessor")]
    public void CheckoutEventsProcessor_CompletePaid(
        [ServiceBusTrigger("stripe-checkout-events","checkout-complete-paid", Connection = "ServicebusConnection")]
        string checkoutQueueItem,
        FunctionContext context)
    {
        if (context.BindingContext.BindingData.TryGetValue("ContentType", out object contentTypeObj) &&
            contentTypeObj is MediaTypeNames.Application.Json)
        {
            var checkoutData = JsonSerializer.Deserialize<CheckoutEventPayload>(checkoutQueueItem);
            var payloadValidationResult = _checkoutEventValidator.Validate(checkoutData);
            if (!payloadValidationResult.IsValid) throw new Exception("Invalid payload");
            
            try
            {
                _logger.LogInformation("Received checkout event {CheckoutSessionID} {Status}", checkoutData.CheckoutId,
                    checkoutData.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to process message");
            }
        }

        _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {checkoutQueueItem}");
    }
}