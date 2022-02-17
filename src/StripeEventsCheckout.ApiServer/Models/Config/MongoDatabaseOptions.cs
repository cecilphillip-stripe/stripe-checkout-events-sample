namespace StripeEventsCheckout.ApiServer.Models.Config;
public class MongoDatabaseOptions
{
    public string? ConnectionString { get; set; }
    public string? DatabaseName { get; set; }
    public string? EventsCollectionName { get; set; }
}