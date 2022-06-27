using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Stripe;
using StripeEventsCheckout.ApiServer.Data;
using StripeEventsCheckout.ApiServer.Models.Config;

namespace StripeEventsCheckout.ApiServer;

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
}