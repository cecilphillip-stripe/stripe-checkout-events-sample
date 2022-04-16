namespace StripeEventsCheckout.BlazorUI.Models;

public class AppState
{
    public EventInfo? CurrentEvent { get; set; }
}


public record CheckSessionResponse(string Url);