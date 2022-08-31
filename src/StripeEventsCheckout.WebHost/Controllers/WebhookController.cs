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
    private readonly IStripeClient _stripeClient;
    private readonly IMessageSender _messenger;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOptions<StripeOptions> _stripeConfig;

    public WebhookController(IStripeClient stripeClient, IMessageSender messenger, ILogger<WebhookController> logger,
        IOptions<StripeOptions> stripeConfig)
    {
        _stripeClient = stripeClient;
        _messenger = messenger;
        _logger = logger;
        _stripeConfig = stripeConfig;
    }

    [HttpPost]
    public async Task<ActionResult> Handler()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload,
                Request.Headers["Stripe-Signature"],
                _stripeConfig.Value.WebhookSecret, throwOnApiVersionMismatch:false
            );

            _logger.LogInformation($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");

            switch (stripeEvent.Type)
            {
                // Handle the events
                case Events.CheckoutSessionCompleted:
                {
                    var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    _logger.LogInformation("Checkout.Session ID: {CheckoutId}, Status: {CheckoutSessionStatus}", checkoutSession!.Id, checkoutSession.Status);

                    if (checkoutSession.Status == "complete" && checkoutSession.PhoneNumberCollection.Enabled)
                    {
                        try
                        {
                            var recipient = _messenger switch
                            {
                                TwilioMessageSender => checkoutSession.CustomerDetails.Phone,
                                SendGridMessageSender => checkoutSession.CustomerDetails.Email,
                                _ => throw new ArgumentException("Unsupported Type")
                            };

                            await _messenger.SendMessageAsync("Your order has been successfully processed", recipient);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }

                    break;
                }
                case Events.CheckoutSessionExpired:
                {
                    var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    _logger.LogInformation($"Checkout.Session ID: {checkoutSession!.Id}");

                    // Notify your customer about the cart
                    break;
                }
                case Events.CheckoutSessionAsyncPaymentSucceeded:
                {
                    _logger.LogInformation("Deferred payment");
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
    }
}