using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using StripeEventsCheckout.ApiServer.Models.Config;

namespace StripeEventsCheckout.ApiServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookController> _logger;
    private readonly IOptions<StripeOptions> _stripeConfig;

    public WebhookController(IHttpClientFactory httpClientFactory, ILogger<WebhookController> logger,
        IOptions<StripeOptions> stripeConfig)
    {
        _httpClientFactory = httpClientFactory;
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
                _stripeConfig.Value.WebhookSecret
            );

            _logger.LogInformation("Webhook notification with type: {StripeEventType} found for {StripeEventId}", stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case Events.CheckoutSessionCompleted or Events.CheckoutSessionAsyncPaymentSucceeded:
                    {
                        var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        _logger.LogInformation("Checkout.Session ID: {Id}, Status: {CheckoutSessionStatus} PaymentStatus: {CheckoutSessionPaymentStatus}", checkoutSession!.Id, checkoutSession.Status, checkoutSession.PaymentStatus);

                        if (checkoutSession.Status == "complete" && checkoutSession.PhoneNumberCollection.Enabled)
                        {
                            var daprHttpClient = _httpClientFactory.CreateClient("dapr");
                            var pubMessage = JsonSerializer.Serialize(new
                            {
                                StripeSessionId = checkoutSession.Id 
                            });
                            
                            var content = new StringContent(pubMessage, Encoding.UTF8, "application/json");
                            await daprHttpClient.PostAsync($"v1.0/publish/{_stripeConfig.Value.PubSubName}/fulfill.order", content);
                        }

                        break;
                    }
                case Events.CheckoutSessionExpired:
                    {
                        var checkoutSession = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        _logger.LogInformation("Checkout.Session ID: {Id}", checkoutSession!.Id);

                        // Notify your customer about the cart
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