using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using StripeEventsCheckout.ApiServer.Models.Config;

namespace StripeEventsCheckout.ApiServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IOptions<StripeOptions> _stripeConfig;

    public WebhookController(ILogger<WebhookController> logger, IOptions<StripeOptions> stripeConfig)
    {
        _logger = logger;
        _stripeConfig = stripeConfig;
    }

    [HttpPost]
    public async Task<ActionResult> Handler()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload,
                Request.Headers["Stripe-Signature"],
                _stripeConfig.Value.WebhookSecret
            );

            _logger.LogInformation($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");

            // Handle the events
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id}, Status: {checkoutSession.Status}");
            }

            else if (stripeEvent.Type == Events.CheckoutSessionExpired)
            {
                var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id}");

                // Notify your customer about the cart
            }
            else
            {
                _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest();
        }
    }
}