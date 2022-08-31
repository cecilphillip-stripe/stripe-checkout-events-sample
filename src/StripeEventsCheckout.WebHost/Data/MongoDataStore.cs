using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StripeEventsCheckout.WebHost.Models;
using StripeEventsCheckout.WebHost.Models.Config;

namespace StripeEventsCheckout.WebHost.Data;

public class MongoDataStore : IEventListingDataStore
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly MongoDatabaseOptions _mongodbSettings;

    public MongoDataStore(IMongoClient mongoClient, IOptions<MongoDatabaseOptions> mongodbOptions)
    {
        this._mongodbSettings = mongodbOptions.Value;
        this._mongoClient = mongoClient;
        this._database = this._mongoClient.GetDatabase(_mongodbSettings.DatabaseName);
    }
    public async Task<IEnumerable<EventListing>> GetEventListings(int page, int count)
    {
        var collection = this._database.GetCollection<EventListing>(_mongodbSettings.EventsCollectionName);
        var results = await collection.Find(e => true).Skip((page - 1) * count).Limit(count).ToListAsync<EventListing>();
        return results;
    }

    public async Task<EventListing> GetEventListing(string code)
    {
        var collection = this._database.GetCollection<EventListing>(_mongodbSettings.EventsCollectionName);
        var filter = Builders<EventListing>.Filter.Eq(r => r.EventCode, code);

        var result = await collection.Find(filter)
                        .FirstOrDefaultAsync();
        return result;
    }

    public async Task<IEnumerable<EventListing>> GetEventListingById(string[] ids)
    {
        var collection = this._database.GetCollection<EventListing>(_mongodbSettings.EventsCollectionName);
        var filter = Builders<EventListing>.Filter.In(r => r.EventCode, ids);

        var results = await collection.Find(filter).ToListAsync();
        return results;
    }
}