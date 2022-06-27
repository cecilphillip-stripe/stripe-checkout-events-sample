using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Mvc;
using CloudNative.CloudEvents.AspNetCore;
using CloudNative.CloudEvents.SystemTextJson;
using Stripe;
using StripeEventsCheckout.OrderProcessor.Services;

namespace StripeEventsCheckout.OrderProcessor.Controllers;

[ApiController]
[Route("[controller]")]
public class DaprController : ControllerBase
{
    private readonly IStripeClient _stripeClient;
    private readonly IMessageSender _messenger;
    private readonly ILogger<DaprController> _logger;
    private readonly CloudEventFormatter _formatter;

    public DaprController(IStripeClient stripeClient, IMessageSender messenger, ILogger<DaprController> logger)
    {
        this._stripeClient = stripeClient;
        this._messenger = messenger;
        this._logger = logger;
        this._formatter = new JsonEventFormatter();
    }

    [HttpGet("subscribe")]
    public ActionResult Subscribe()
    {
        var payload = new[]
        {
            new {pubsubname = "rabbitmqbus", topic = "fulfill.order", route = "fulfillment"}
        };
        return Ok(payload);
    }

    [HttpPost("/fulfillment")]
    public async Task<ActionResult> CheckoutOrder()
    {
        try
        {
            CloudEvent cloudEvent = await this.Request.ToCloudEventAsync(_formatter);
            _logger.LogDebug("Cloud event {CloudEventId} {CloudEventType} {CloudEventDataContentType}", cloudEvent.Id,
                cloudEvent.Type, cloudEvent.DataContentType);

            //cloudEvent.
            //_logger.LogInformation("Order received...");


            var customerService = new CustomerService(_stripeClient);
            var customer = await customerService.GetAsync("checkoutSession.CustomerId");

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