using MongoDB.Driver;
using MongoDB.Driver.Linq;
using StripeEventsCheckout.ApiServer.Data;
using StripeEventsCheckout.ApiServer.Models.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContosoMongoDb(this IServiceCollection services, IConfiguration config)
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
}