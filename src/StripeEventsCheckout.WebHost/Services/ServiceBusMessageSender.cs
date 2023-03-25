using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using StripeEventsCheckout.WebHost.Models.Config;

namespace StripeEventsCheckout.WebHost.Services;

public class ServiceBusMessageSender: IMessageSender
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusOptions _sbOptions;
    private readonly ILogger<ServiceBusMessageSender> _logger;

    public ServiceBusMessageSender(ServiceBusClient serviceBusClient, IOptions<ServiceBusOptions> sbOptions, ILogger<ServiceBusMessageSender> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _sbOptions = sbOptions.Value;
    }
    
    public async Task SendMessageAsync(string message, string receiver, string? contentType, IDictionary<string,string>? metadata = null)
    {
        try
        {
            var sender = _serviceBusClient.CreateSender(receiver);
            var sbMessage = new ServiceBusMessage(message)
            {
                Subject = "Stripe Checkout Event",
                ContentType = contentType,
                MessageId = Guid.NewGuid().ToString("N"),
            };
            if (metadata is not null)
            {
                foreach (var (key, value) in metadata)
                    sbMessage.ApplicationProperties.Add(key, value);
            }
            
            sbMessage.ApplicationProperties.Add("demo", "StripeEventsCheckout");

            await sender.SendMessageAsync(sbMessage);
            _logger.LogInformation("Message sent {MessageID} to {DestinationTopicName}", sbMessage.MessageId, _sbOptions.CheckoutEntityName);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to send message to service bus");
        }
    }
}