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

    public Index(ICustomerDataStore dataStore)
    {
        _dataStore = dataStore;
    }
    
    [BindProperty] public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPost()
    {
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

    public string Button { get; set; }
}