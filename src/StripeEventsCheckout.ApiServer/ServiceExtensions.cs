using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Stripe;
using StripeEventsCheckout.ApiServer.Data;
using StripeEventsCheckout.ApiServer.Models.Config;
using Twilio;
using Twilio.Clients;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDbServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDatabaseOptions>(config.GetSection("MongoDatabase"));
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = MongoClientSettings.FromConnectionString(config["MongoDatabase:ConnectionString"]);

            // Setting the version of the Stable API
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            settings.LinqProvider = LinqProvider.V3;
            return new MongoClient(settings);
        });

        services.AddTransient<IEventListingDataStore, MongoDataStore>();

        return services;
    }

    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["SECRET_KEY"];
        services.Configure<StripeOptions>(options =>
        {
            options.WebhookSecret = config["WEBHOOK_SECRET"];
            options.PublicKey = config["PUBLISHABLE_KEY"];
            options.SecretKey = StripeConfiguration.ApiKey;
        });

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
        string accountSid = config["ACCOUNT_SID"];
        string authToken = config["AUTH_TOKEN"];
        string phoneNumber = config["PHONE_NUMBER"];

        services.Configure<TwilioOptions>(options =>
        {
            options.AccountSID = accountSid;
            options.AuthToken = authToken;
            options.PhoneNumber = phoneNumber;
        });

        TwilioClient.Init(accountSid, authToken);

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