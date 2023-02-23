using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using Twilio;
using Twilio.Clients;

namespace StripeEventsCheckout.ServerlessWorker;

public static class Extensions
{
    public static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration config)
    {
        var accountSid = config["Twilio_AccountSID"];
        var authToken = config["Twilio_AuthToken"];
        TwilioClient.Init(accountSid, authToken);

        services.Configure<TwilioOptions>(options =>
        {
            options.AuthToken = config["Twilio_AuthToken"];
            options.AccountSID = config["Twilio_AccountSID"];
            options.PhoneNumber = config["Twilio_PhoneNumber"];
        });

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
    
    public static IServiceCollection AddStripe(this IServiceCollection services,string secretKey)
    {
        StripeConfiguration.ApiKey = secretKey;
        
        var appInfo = new AppInfo
        {
            Name = "StripeEvents (Azure)",
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
}

public class TwilioOptions
{
    public string AccountSID { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}