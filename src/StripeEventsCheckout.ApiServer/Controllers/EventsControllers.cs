using Microsoft.AspNetCore.Mvc;

namespace StripeEventsCheckout.ApiServer.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsControllers : ControllerBase
{
    private readonly ILogger<EventsControllers> _logger;

    public EventsControllers(ILogger<EventsControllers> logger)
    {
        _logger = logger;
    }
}
