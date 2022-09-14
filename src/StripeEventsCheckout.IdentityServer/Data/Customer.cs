using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StripeEventsCheckout.IdentityServer.Data;

[BsonIgnoreExtraElements]
public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [BsonElement("id")]
    public string CustomerId { get; set; }
    public string StripeCustomerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string Country { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<string> Roles { get; set; }
}