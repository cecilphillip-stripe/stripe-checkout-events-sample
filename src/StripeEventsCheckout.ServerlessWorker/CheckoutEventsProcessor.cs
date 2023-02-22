using System;
using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
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
    public async Task CheckoutEventsProcessor_CompletePaid(
        [ServiceBusTrigger("stripe-checkout-events","checkout-complete-paid", Connection = "ServicebusConnection")]
        CheckoutEventPayload checkoutData,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        if (context.BindingContext.BindingData.TryGetValue("ContentType", out object contentTypeObj) &&
            contentTypeObj is MediaTypeNames.Application.Json)
        {
            var payloadValidationResult = _checkoutEventValidator.Validate(checkoutData);
            if (!payloadValidationResult.IsValid) throw new Exception("Invalid payload");
            
            try
            {
                _logger.LogInformation("Received checkout event {CheckoutSessionID} {Status}", checkoutData.CheckoutId,
                    checkoutData.Status);
                var workflowInstanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(DurableOrchestration.CheckoutEvents_FulfillmentOrchestration), checkoutData);
                
                _logger.LogInformation("Workflow started => {WorkflowInstance}", workflowInstanceId);
                //TODO: Signal workflow started successfully or failed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to process message");
            }
        }
    }
}