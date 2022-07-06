using System.Text.Json;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Mvc;
using CloudNative.CloudEvents.AspNetCore;
using CloudNative.CloudEvents.SystemTextJson;
using Stripe;
using Stripe.Checkout;
using StripeEventsCheckout.OrderProcessor.Services;

namespace StripeEventsCheckout.OrderProcessor.Controllers;

[ApiController]
[Route("[controller]")]
public class DaprController : ControllerBase
{
    private readonly IStripeClient _stripeClient;
    private readonly IMessageSender _messenger;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DaprController> _logger;
    private readonly CloudEventFormatter _formatter;

    public DaprController(IStripeClient stripeClient, IMessageSender messenger, IConfiguration configuration, ILogger<DaprController> logger)
    {
        this._stripeClient = stripeClient;
        this._messenger = messenger;
        this._configuration = configuration;
        this._logger = logger;
        this._formatter = new JsonEventFormatter();
    }

    [HttpGet("subscribe")]
    public ActionResult Subscribe()
    {
        var pubSubConfig = _configuration.GetSection("DaprPubSub");
        
        var payload = new[]
        {
            new
            {
                pubsubname = pubSubConfig.GetValue<string>("Name"), 
                topic = pubSubConfig.GetValue<string>("Topic"),
                route = pubSubConfig.GetValue<string>("Route")
            }
        };
        
        return Ok(payload);
    }

    [HttpPost("/fulfillment")]
    public async Task<ActionResult> CheckoutOrder()
    {
        try
        {
            var cloudEvent = await this.Request.ToCloudEventAsync(_formatter);
            _logger.LogDebug("Cloud event {CloudEventId} {CloudEventType} {CloudEventDataContentType}", cloudEvent.Id,
                cloudEvent.Type, cloudEvent.DataContentType);

            if (cloudEvent.Data is not JsonElement rawDataPayload) return BadRequest();

            var dataPayload = rawDataPayload.Deserialize<CheckoutDataResponse>();
            var sGetOptions = new SessionGetOptions
            {
                Expand = new List<string> {"customer"}
            };
            
            var sessionService = new SessionService(_stripeClient);
            var checkoutSession = await sessionService.GetAsync(dataPayload!.StripeSessionId, sGetOptions);
            var customer = checkoutSession.Customer;

            _logger.LogInformation("Order received...");
            var recipient = _messenger switch
            {
                TwilioMessageSender => customer.Phone,
                SendGridMessageSender => customer.Email,
                _ => throw new ArgumentException("Unsupported Type")
            };

            await _messenger.SendMessageAsync("Your order has been successfully processed", recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return Ok();
    }
}

public record CheckoutDataResponse(string StripeSessionId);