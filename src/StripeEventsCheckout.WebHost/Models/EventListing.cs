namespace StripeEventsCheckout.WebHost.Models;

public class EventListing
{
    public string EventCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public uint Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public NestedHost Host { get; set; } = new();
    public NestedVenue Venue { get; set; } = new();

    public class NestedHost
    {
        public string Company { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class NestedVenue
    {
        public string City { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
    }
}