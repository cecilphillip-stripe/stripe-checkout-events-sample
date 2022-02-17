namespace StripeEventsCheckout.ApiServer.Models;

public class EventListing
{
    public string EventCode { get; set; }
    public string Name { get; set; }
    public uint Price { get; set; }
    public string ImageUrl { get; set; }

    public NestedHost Host { get; set; }
    public NestedVenue Venue { get; set; }

    public class NestedHost
    {
        public string Company { get; set; }
        public string Email { get; set; }
    }

    public class NestedVenue
    {
        public string City { get; set; }
        public string StateCode { get; set; }
    }
}