using System.ComponentModel.DataAnnotations;

namespace StripeEventsCheckout.WebHost.Models;

public class AuthenticationRequest
{
    [Required]
    public string? Email { get; init; }
    
    [Required]
    public string? Password { get; init; }
}

public record UserInfo(string Id, string UserName, string Email);
