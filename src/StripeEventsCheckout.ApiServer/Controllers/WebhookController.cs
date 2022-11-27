using System.Net.Mime;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using StripeEventsCheckout.ApiServer.Models;
using StripeEventsCheckout.ApiServer.Models.Config;

namespace StripeEventsCheckout.ApiServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IOptions<StripeOptions> _stripeConfig;

    public WebhookController(IAmazonSQS sqsClient, ILogger<WebhookController> logger,
         IOptions<StripeOptions> stripeConfig)
    {
        _logger = logger;
        _sqsClient = sqsClient;
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
                            var cloudEvent = new CloudEvent
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                Type = $"stripe.{Events.CheckoutSessionCompleted}",
                                Source = new Uri("urn:stripeEventsCheckout:apiServer:webhook"),
                                DataContentType = MediaTypeNames.Application.Json,
                                Data = new QueueMessagePayload(checkoutSession.Id, checkoutSession.Status)
                            };
                            
                            var cloudEventFormatter = new JsonEventFormatter<QueueMessagePayload>();
                            var encodedMsg = cloudEventFormatter.EncodeStructuredModeMessage(cloudEvent, out ContentType contentType);
                            var encodedMsgStr = Encoding.UTF8.GetString(encodedMsg.Span);
                            
                            var sqsMessage = new SendMessageRequest
                            {
                                QueueUrl = "https://sqs.us-east-1.amazonaws.com/475533875648/stripe-events",
                                MessageBody = encodedMsgStr,
                                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                                {
                                    ["contentType"] = new() {StringValue = contentType.ToString(), DataType = "String" }
                                }
                            };
                            var sqsResponse = await _sqsClient.SendMessageAsync(sqsMessage);
                            _logger.LogInformation("SQS Response Status Code => {SqsResponseHttpStatusCode}", sqsResponse.HttpStatusCode);

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
                    _logger.LogInformation("Checkout.Session ID: {Id}", checkoutSession!.Id);

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