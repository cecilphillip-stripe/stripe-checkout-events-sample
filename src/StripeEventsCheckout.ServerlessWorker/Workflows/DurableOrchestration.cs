using System.Text;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using ShipEngineSDK;
using ShipEngineSDK.Common;
using ShipEngineSDK.Common.Enums;
using ShipEngineSDK.CreateLabelFromShipmentDetails;
using Stripe;
using Stripe.Checkout;
using StripeEventsCheckout.ServerlessWorker.Services;
using static LanguageExt.Prelude;
using Address = ShipEngineSDK.Common.Address;
using ShipResult = ShipEngineSDK.CreateLabelFromShipmentDetails.Result;

namespace StripeEventsCheckout.ServerlessWorker.Workflows;

#pragma warning disable CA2208
public class DurableOrchestration
{
    private readonly IStripeClient _stripeClient;
    private readonly SendGridNotifier _sendGridNotifier;
    private readonly TwilioNotifier _twilioNotifier;
    private readonly ShipEngine _shipEngine;
    private readonly TaskOptions _taskOptions;

    public DurableOrchestration(IStripeClient stripeClient, SendGridNotifier sendGridNotifier,
        TwilioNotifier twilioNotifier, ShipEngine shipEngine)
    {
        _stripeClient = stripeClient;
        _sendGridNotifier = sendGridNotifier;
        _twilioNotifier = twilioNotifier;
        _shipEngine = shipEngine;
        _taskOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(Int32.MaxValue, TimeSpan.FromSeconds(1.5)));
    }

    [Function(nameof(CheckoutEvents_FulfillmentOrchestration))]
    public async Task CheckoutEvents_FulfillmentOrchestration([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var checkoutData = context.GetInput<CheckoutEventPayload>();
        if (checkoutData is null) throw new ArgumentNullException(nameof(checkoutData));

        var logger = context.CreateReplaySafeLogger(nameof(CheckoutEvents_FulfillmentOrchestration));

        try
        {
            await context.CallActivityAsync<ActivityResult<bool>>(nameof(CheckoutEvents_SendOrderConfirmation),
                checkoutData, _taskOptions);
            await context.CallActivityAsync<ActivityResult<Unit>>(nameof(CheckoutEvents_UpdateInventory), checkoutData,
                _taskOptions);

            var sdResult =
                await context.CallActivityAsync<ActivityResult<string>>(nameof(CheckoutEvents_ScheduleDelivery),
                    checkoutData, _taskOptions);
            if (sdResult.IsFailure)
            {
                logger.LogError("Couldn't schedule delivery for Orchestration instance {{{InstanceId}}}. ",
                    context.InstanceId);
                context.SetCustomStatus("DeliverySchedulingFailed");
                throw new Exception($"Couldn't schedule delivery for Orchestration instance {context.InstanceId}");
            }

            checkoutData.TrackingCode = sdResult.Value!;
            await context.CallActivityAsync<ActivityResult<bool>>(nameof(CheckoutEvents_UpdateCustomer), checkoutData,
                _taskOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Orchestration instance {{{InstanceId}}} failed ", context.InstanceId);
        }
    }

    [Function(nameof(CheckoutEvents_SendOrderConfirmation))]
    public async Task<ActivityResult<bool>> CheckoutEvents_SendOrderConfirmation(
        [ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_SendOrderConfirmation));

        logger.LogInformation("Retrieving checkout session information");
        var sessionService = new SessionService(_stripeClient);
        var customerSession = await sessionService.GetAsync(checkoutData.CheckoutId);

        if (customerSession is null)
        {
            return new Result<bool>(
                new Exception($"Checkout session {{{checkoutData.CheckoutId}}} not found.")
            );
        }

        logger.LogInformation("Sending customer notification");
        const string message = "Thanks for your order! You'll be receiving your ticket(s) soon 🙂";
        return await _twilioNotifier.SendMessageAsync(message, customerSession.CustomerDetails.Phone);
    }

    [Function(nameof(CheckoutEvents_UpdateInventory))]
    public async Task<ActivityResult<Unit>> CheckoutEvents_UpdateInventory(
        [ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateInventory));
        logger.LogInformation("Updating inventory");
        await Task.Delay(TimeSpan.FromSeconds(5));
        return new Result<Unit>(Unit.Default);
    }

    [Function(nameof(CheckoutEvents_ScheduleDelivery))]
    public async Task<ActivityResult<string>> CheckoutEvents_ScheduleDelivery(
        [ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_ScheduleDelivery));
        logger.LogInformation("Scheduling delivery. Generating ticket(s)...");

        var sessionService = new SessionService(_stripeClient);
        var customerSession = await sessionService.GetAsync(checkoutData.CheckoutId);

        if (customerSession is null)
        {
            return new Result<string>(
                new Exception($"Checkout session {{{checkoutData.CheckoutId}}} not found.")
            );
        }

        var rateParams = new Params
        {
            LabelLayout = LabelLayout.FourBySix,
            Shipment = new Shipment
            {
                ServiceCode = "ups_ground",
                ShipFrom = new Address
                {
                    Name = "Stripe Checkout Events Demo",
                    AddressLine1 = "Oyster Point Blvd",
                    CityLocality = "South San Francisco",
                    StateProvince = "CA", PostalCode = "94080",
                    CountryCode = Country.US, Phone = "555-555-5555"
                },
                ShipTo = new Address
                {
                    Name = customerSession.ShippingDetails.Name,
                    AddressLine1 = customerSession.ShippingDetails.Address.Line1,
                    CityLocality = customerSession.ShippingDetails.Address.City,
                    StateProvince = customerSession.ShippingDetails.Address.State,
                    PostalCode = customerSession.ShippingDetails.Address.PostalCode,
                    CountryCode = Country.US,
                    Phone = customerSession.CustomerDetails.Phone
                },
                Packages = new List<Package>
                {
                    new Package
                    {
                        Weight = new Weight
                        {
                            Value = 5,
                            Unit = WeightUnit.Ounce
                        },
                        Dimensions = new Dimensions
                        {
                            Length = 36,
                            Width = 12,
                            Height = 24,
                            Unit = DimensionUnit.Inch,
                        }
                    }
                }
            }
        };

        var shipResults = await TryAsync(async () => await _shipEngine.CreateLabelFromShipmentDetails(rateParams))();

        var deliveryResult = shipResults.Match(
            r => new Result<string>(r.TrackingNumber ?? string.Empty),
            e => new Result<string>(e)
        );
        return deliveryResult;
    }

    [Function(nameof(CheckoutEvents_UpdateCustomer))]
    public async Task<ActivityResult<bool>> CheckoutEvents_UpdateCustomer(
        [ActivityTrigger] CheckoutEventPayload checkoutData,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckoutEvents_UpdateCustomer));
        var sessionService = new SessionService(_stripeClient);
        var customerSession = await sessionService.GetAsync(checkoutData.CheckoutId);

        if (customerSession is null)
        {
            return new Result<bool>(
                ErrorException.New($"Checkout session {{{checkoutData.CheckoutId}}} not found.")
            );
        }

        logger.LogInformation("Emailing tickets to customer");
        var sb = new StringBuilder("Here are your tickets.");

        if (!string.IsNullOrEmpty(checkoutData.TrackingCode))
            sb.Append($"Use this tracking code to track your order {checkoutData.TrackingCode}");

        var messageSentResult =
            await _sendGridNotifier.SendMessageAsync(sb.ToString(), customerSession.CustomerDetails.Email);
        return messageSentResult;
    }
}