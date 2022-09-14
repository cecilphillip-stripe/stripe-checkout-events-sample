namespace StripeEventsCheckout.BlazorUI.Auth;

public class AntiForgeryHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-CSRF", "1");
        request.Headers.Add("X-CLIENT-SOURCE", nameof(BlazorUI));
        return base.SendAsync(request, cancellationToken);
    }
}