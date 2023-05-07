using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using StripeEventsCheckout.WebHost.Models.Config;
using StripeEventsCheckout.WebHost.Services;

namespace StripeEventsCheckout.WebHost.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IMessageSender _messenger;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOptions<StripeOptions> _stripeConfig;
    private readonly IOptions<ServiceBusOptions> _sbConfig;

    public WebhookController(IMessageSender messenger, ILogger<WebhookController> logger,
        IOptions<StripeOptions> stripeConfig, IOptions<ServiceBusOptions> sbConfig)
    {
        _messenger = messenger;
        _logger = logger;
        _stripeConfig = stripeConfig;
        _sbConfig = sbConfig;
    }

    [HttpPost]
    public async Task<ActionResult> Handler()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload,
                Request.Headers["Stripe-Signature"],
                _stripeConfig.Value.WebhookSecret, throwOnApiVersionMismatch: false
            );

            _logger.LogInformation($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");

            switch (stripeEvent.Type)
            {
                // Handle the events
                case Events.CheckoutSessionCompleted:
                {
                    var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    _logger.LogInformation("Checkout.Session ID: {CheckoutId}, Status: {CheckoutSessionStatus}",
                        checkoutSession!.Id, checkoutSession.Status);

                    if (checkoutSession is { Status: "complete", PaymentStatus: "paid" })
                    {
                        var messageData = new EventPayload(checkoutSession.Id, checkoutSession.Status,
                            Events.CheckoutSessionCompleted);
                        var serializedMessageData = JsonSerializer.Serialize(messageData);

                        await _messenger.SendMessageAsync(serializedMessageData, _sbConfig.Value.CheckoutEntityName,
                            MediaTypeNames.Application.Json, new Dictionary<string, string>
                            {
                                ["stripe-event"] = messageData.Event,
                                ["payment-status"] = "paid"
                            });
                    }

                    break;
                }
                
                case Events.CheckoutSessionExpired:
                {
                    var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id} expired");

                    var messageData = new EventPayload(checkoutSession.Id, checkoutSession.Status,
                        Events.CheckoutSessionExpired);
                    var serializedMessageData = JsonSerializer.Serialize(messageData);

                    await _messenger.SendMessageAsync(serializedMessageData, _sbConfig.Value.CheckoutEntityName,
                        MediaTypeNames.Application.Json, new Dictionary<string, string>
                        {
                            ["stripe-event"] = messageData.Event
                        });

                    break;
                }
                default:
                    _logger.LogInformation("Unhandled event type: {StripeEvent}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to process webhook");
            return BadRequest();
        }
    }
}

// ReSharper disable once NotAccessedPositionalProperty.Global
record EventPayload(string CheckoutId, string Status, string Event);