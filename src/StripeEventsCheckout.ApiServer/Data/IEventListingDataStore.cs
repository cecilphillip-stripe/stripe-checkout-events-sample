using StripeEventsCheckout.ApiServer.Models;

namespace StripeEventsCheckout.ApiServer.Data;

public interface IEventListingDataStore
{
    Task<EventListing> GetEventListing(string code);
    Task<IEnumerable<EventListing>> GetEventListingById(string[] codes);
    Task<IEnumerable<EventListing>> GetEventListings(int page, int count);
}
