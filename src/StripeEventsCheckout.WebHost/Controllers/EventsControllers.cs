using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace StripeEventsCheckout.WebHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IStripeClient _stripeClient;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IStripeClient stripeClient, ILogger<EventsController> logger)
    {
        _stripeClient = stripeClient;
        _logger = logger;
    }

    [HttpGet("available")]
    public async Task<ActionResult> GetUpcomingEvents()
    {
        var options = new PriceListOptions
        {
            Limit = 40,
            Active = true,
            Expand = new List<string> { "data.product" },
            Type = "one_time"
        };
        var priceService = new PriceService(_stripeClient);
        var prices = await priceService.ListAsync(options);

        var flattenPriceData = prices.Select(p => new
            {
                price_id = p.Id,
                product_id = p.ProductId,
                name = p.Product.Name,
                owner = p.Product.Metadata["ownerName"],
                unit_amount = p.UnitAmount,
                images = p.Product.Images
            });

        return Ok(flattenPriceData);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult> CreateCheckoutSession(CheckoutSessionRequest payload)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sessionCreateOptions = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = payload.PriceId,
                    Quantity = payload.Quantity,
                },
            },
            Mode = "payment",
            ConsentCollection = new()
            {
                Promotions = "auto"
            },
            PhoneNumberCollection = new()
            {
                Enabled = true,
            },
            ShippingAddressCollection = new()
            {
                AllowedCountries = new List<string> { "US" }
            },
            //SuccessUrl = baseUrl + "/success?session_id={CHECKOUT_SESSION_ID}",
            SuccessUrl = baseUrl + $"/success",
            CancelUrl = baseUrl,
        };

        if (HttpContext.User?.Identity?.IsAuthenticated ?? false)
        {
            var stripeCustomerIdClaim =
                (HttpContext.User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "stripe_customer");
            if (stripeCustomerIdClaim is not null)
            {
                sessionCreateOptions.Customer = stripeCustomerIdClaim.Value;
            }
        }

        var sessionService = new SessionService(_stripeClient);
        var session = await sessionService.CreateAsync(sessionCreateOptions);

        return Ok(new { session.Url });
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult> GetEventByPriceId(string productId)
    {
        var options = new PriceListOptions()
        {
            Product = productId,
            Limit = 4,
            Active = true,
            Expand = new List<string> { "data.product" }
        };
        var priceService = new PriceService(_stripeClient);
        var priceResult = await priceService.ListAsync(options);

        var flattenPriceData = priceResult.Select(p => new
        {
            price_id = p.Id,
            product_id = p.ProductId,
            name = p.Product.Name,
            owner = p.Product.Metadata["owner"],
            unit_amount = p.UnitAmount,
            images = p.Product.Images
        });

        return Ok(flattenPriceData.FirstOrDefault());
    }
}

public record CheckoutSessionRequest(string PriceId, int Quantity);