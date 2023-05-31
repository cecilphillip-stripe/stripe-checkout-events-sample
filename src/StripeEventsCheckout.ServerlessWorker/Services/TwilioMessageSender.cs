using LanguageExt.Common;
using static LanguageExt.Prelude;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;

namespace StripeEventsCheckout.ServerlessWorker.Services;

public class TwilioNotifier
{
    private readonly ITwilioRestClient _twilioRestClient;
    private readonly IOptions<TwilioOptions> _twilioOptions;

    public TwilioNotifier(ITwilioRestClient twilioRestClient, IOptions<TwilioOptions> twilioOptions)
    {
        _twilioRestClient = twilioRestClient;
        _twilioOptions = twilioOptions;
    }
    public async Task<Result<bool>> SendMessageAsync(string message, string receiver)
    {
        var messageTask = MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_twilioOptions.Value.PhoneNumber),
            to: new Twilio.Types.PhoneNumber(receiver),
            client: _twilioRestClient
        );

        var sentMessage = await TryAsync(async () => await messageTask)();
        
        return sentMessage.Match(
            resp => resp.Status != MessageResource.StatusEnum.Failed && resp.Status != MessageResource.StatusEnum.Canceled?
                true : new Result<bool>(false),
            e => new Result<bool>(e)
        );
    }
}