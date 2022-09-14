using System.Threading.Channels;
using StripeEventsCheckout.IdentityServer.Data;

namespace StripeEventsCheckout.IdentityServer;

public class ChannelNotifier
{
    private readonly Channel<Customer> _customerChannel = Channel.CreateUnbounded<Customer>(new()
    { SingleReader = true,SingleWriter = true });

    public ChannelWriter<Customer> CreateStripeAccountWriter => _customerChannel.Writer;
    public ChannelReader<Customer> CreateStripeAccountReader => _customerChannel.Reader;
}