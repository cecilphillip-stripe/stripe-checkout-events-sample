using System.Security.Claims;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using StripeEventsCheckout.IdentityServer.Data.MongoData;

namespace StripeEventsCheckout.IdentityServer.Data;

public class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly ICustomerDataStore _dataStore;
    private readonly ISystemClock _clock;

    public CustomResourceOwnerPasswordValidator(ICustomerDataStore dataStore, ISystemClock clock)
    {
        _dataStore = dataStore;
        _clock = clock;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var customer = await _dataStore.GetCustomerByUsername(context.UserName);
        //DO NOT USE THIS
        //ignores passwords, but you probably should
        if (customer is not null)
        {
            var claims = new List<Claim>
            {
                new(JwtClaimTypes.Name, $"{customer.FirstName} {customer.LastName}"),
                new(JwtClaimTypes.GivenName, customer.FirstName),
                new(JwtClaimTypes.FamilyName, customer.LastName),
                new(JwtClaimTypes.Subject, customer.CustomerId),
                new(JwtClaimTypes.Email, customer.Email),
                new(JwtClaimTypes.EmailVerified, "true")
            };

            claims.AddRange(customer.Roles.Select(role => new Claim(JwtClaimTypes.Role, role)));

            context.Result = new GrantValidationResult(customer.CustomerId,
                OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime, claims);
        }
    }
}