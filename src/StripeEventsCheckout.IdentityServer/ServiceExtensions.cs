using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using StripeEventsCheckout.IdentityServer.Data.MongoData;

namespace StripeEventsCheckout.IdentityServer;

public static class ServiceExtensions
{
    public static IServiceCollection AddMongoDataStore(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDbOptions>(config);
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = MongoClientSettings.FromConnectionString(config.GetValue<string>("ConnectionString"));

            // Setting the version of the Stable API
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            settings.LinqProvider = LinqProvider.V3;
            return new MongoClient(settings);
        });
        
        var pack = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("Custom Conventions", pack, t => true);

        services.AddTransient<ICustomerDataStore, MongoDataStore>();
        return services;
    }
}