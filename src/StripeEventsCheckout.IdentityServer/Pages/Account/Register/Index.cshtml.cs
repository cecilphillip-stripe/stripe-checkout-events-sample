using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StripeEventsCheckout.IdentityServer.Data;
using StripeEventsCheckout.IdentityServer.Data.MongoData;

namespace StripeEventsCheckout.IdentityServer.Pages.Account.Register;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly ICustomerDataStore _dataStore;
    private readonly ChannelNotifier _notifier;

    public Index(ICustomerDataStore dataStore, ChannelNotifier notifier)
    {
        _dataStore = dataStore;
        _notifier = notifier;
    }
    
    [BindProperty] public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPost()
    {
        //TODO: check form validation
        if (Input.Button == "cancel")
        {
            Input = new();
        }
        else if (Input.Button == "register")
        {
            var customer = new Customer
            {
               FirstName = Input.FirstName,
               LastName = Input.LastName,
               Email = Input.Email,
               UserName = Input.Username,
               IsActive = true,
               Roles = new []{"customer"}
            };
            
            await _dataStore.CreateCustomer(customer);

            if (Input.CreateStripeCustomer)
            {
               await  _notifier.CreateStripeAccountWriter.WriteAsync(customer);
            }
        }

        return Redirect("~/");
    }
}

public class InputModel
{
    [Required] public string Username { get; set; }
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    [Required] public string Email { get; set; }
    
    [Display(Name = "Create Stripe Customer")]
    [Required] public bool CreateStripeCustomer { get; set; }

    public string Button { get; set; }
}