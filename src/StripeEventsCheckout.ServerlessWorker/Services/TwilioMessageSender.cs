using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;

namespace StripeEventsCheckout.ServerlessWorker.Services;

public class TwilioNotifier : INotifier
{
    private readonly ITwilioRestClient _twilioRestClient;
    private readonly IOptions<TwilioOptions> _twilioOptions;

    public TwilioNotifier(ITwilioRestClient twilioRestClient, IOptions<TwilioOptions> twilioOptions)
    {
        _twilioRestClient = twilioRestClient;
        _twilioOptions = twilioOptions;
    }
    public async Task SendMessageAsync(string message, string receiver)
    {
        var sentMessage = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_twilioOptions.Value.PhoneNumber),
            to: new Twilio.Types.PhoneNumber(receiver),
            client: _twilioRestClient
        );
    }
}