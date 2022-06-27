using Stripe;
using Twilio;
using Twilio.Clients;

namespace StripeEventsCheckout.OrderProcessor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["SecretKey"];
        services.Configure<StripeOptions>(config);

        var appInfo = new AppInfo
        {
            Name = "StripeEvents",
            Version = "0.1.0"
        };
        StripeConfiguration.AppInfo = appInfo;

        services.AddHttpClient("Stripe");
        services.AddTransient<IStripeClient, StripeClient>(s =>
        {
            var clientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = new SystemNetHttpClient(
               httpClient: clientFactory.CreateClient("Stripe"),
               maxNetworkRetries: StripeConfiguration.MaxNetworkRetries,
               appInfo: appInfo,
               enableTelemetry: StripeConfiguration.EnableTelemetry);

            return new StripeClient(apiKey: StripeConfiguration.ApiKey, httpClient: httpClient);
        });

        return services;
    }

    public static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration config)
    {
        string accountSid = config["AccountSID"];
        string authToken = config["AuthToken"];
        TwilioClient.Init(accountSid, authToken);

        services.Configure<TwilioOptions>(config);
        services.AddHttpClient("Twilio");
        services.AddTransient<ITwilioRestClient, TwilioRestClient>(s =>
        {
            var clientFactory = s.GetRequiredService<IHttpClientFactory>();
            var twilioRestClient = new TwilioRestClient(accountSid, authToken,
                     httpClient: new Twilio.Http.SystemNetHttpClient(clientFactory.CreateClient("Twilio")));

            TwilioClient.SetRestClient(twilioRestClient);

            return twilioRestClient;
        });
        return services;
    }
}