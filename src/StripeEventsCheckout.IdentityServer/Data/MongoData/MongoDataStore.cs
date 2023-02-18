using System.Text.RegularExpressions;
using Bogus;
using CountryData.Bogus;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace StripeEventsCheckout.IdentityServer.Data.MongoData;

public class MongoDataStore : ICustomerDataStore
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbOptions _mongodbSettings;
    private readonly Faker<Customer> _faker;

    public MongoDataStore(IMongoClient mongoClient, IOptions<MongoDbOptions> mongodbOptions)
    {
        _mongodbSettings = mongodbOptions.Value;
        _database = mongoClient.GetDatabase(_mongodbSettings.DatabaseName);
        _faker = new Faker<Customer>("en_US").StrictMode(false)
            .RuleFor(u => u.CustomerId, (f, u) => $"cust-{string.Join("", f.Random.Digits(5))}")
            .RuleFor(u => u.PhoneNumber, (f, u) => f.Phone.PhoneNumber("321-###-####"))
            .RuleFor(u => u.Country, (f, u) => f.Country().UnitedStates().Name );
    }

    public async Task<Customer> GetCustomerByUsername(string username)
    {
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        var filter = Builders<Customer>.Filter.Regex(c => c.UserName,
            new(new Regex(username, RegexOptions.IgnoreCase)));

        var result = await collection.Find(filter)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task<Customer> GetCustomerById(string id)
    {
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        var result = await collection.Find(c => c.CustomerId == id)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task CreateCustomer(Customer newCustomer)
    {
        var customerFiller = _faker.Generate();
        newCustomer.CustomerId = customerFiller.CustomerId;
        newCustomer.PhoneNumber = customerFiller.PhoneNumber;
        newCustomer.Country = customerFiller.Country;
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        await collection.InsertOneAsync(newCustomer);
    }

    public async Task<bool> SetStripeCustomerInfo(string customerId, string stripeCustomerId)
    {
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        var filter = Builders<Customer>.Filter.Eq(c => c.CustomerId, customerId );
        var update = Builders<Customer>.Update.Set(c => c.StripeCustomerId, stripeCustomerId);
        var updateResult= await collection.UpdateOneAsync(filter, update);
        return updateResult.IsAcknowledged && updateResult.ModifiedCount == 1;
    }

    public async Task<bool> DeleteCustomerByUsername(string username)
    {
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        var filter = Builders<Customer>.Filter.Regex(c => c.UserName,
            new(new Regex(username, RegexOptions.IgnoreCase)));

        var result = await collection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<bool> DeleteCustomerById(string id)
    {
        var collection = _database.GetCollection<Customer>(_mongodbSettings.CustomersCollectionName);
        var filter = Builders<Customer>.Filter.Eq(p => p.CustomerId, id);
        var result = await collection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}