namespace StripeEventsCheckout.IdentityServer.Data.MongoData;

public class MongoDbOptions
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "eventsshop";
    public string EventsCollectionName { get; set; } = "listings";
    public string CustomersCollectionName { get; set; } = "customers";
}