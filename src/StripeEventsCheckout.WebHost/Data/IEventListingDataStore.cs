using StripeEventsCheckout.WebHost.Models;

namespace StripeEventsCheckout.WebHost.Data;

public interface IEventListingDataStore
{
    Task<EventListing> GetEventListing(string code);
    Task<IEnumerable<EventListing>> GetEventListingById(string[] codes);
    Task<IEnumerable<EventListing>> GetEventListings(int page, int count);
}
