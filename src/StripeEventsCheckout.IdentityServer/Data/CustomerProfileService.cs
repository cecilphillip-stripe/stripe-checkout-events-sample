using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityModel;
using StripeEventsCheckout.IdentityServer.Data.MongoData;

namespace StripeEventsCheckout.IdentityServer.Data;

public class CustomerProfileService: IProfileService
{
    private readonly ICustomerDataStore _dataStore;
    private readonly ILogger<CustomerProfileService> _logger;

    public CustomerProfileService(ICustomerDataStore dataStore, ILogger<CustomerProfileService> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        context.LogProfileRequest(_logger);
        
        if (context.RequestedClaimTypes.Any())
        {
            var customer = await _dataStore.GetCustomerById(context.Subject.GetSubjectId());
            if (customer != null)
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
                if (!string.IsNullOrEmpty(customer.StripeCustomerId))
                {
                    claims.Add(new("stripe_customer",customer.StripeCustomerId ));
                }

                context.AddRequestedClaims(claims);
            }
        }

        context.LogIssuedClaims(_logger);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var customer = await _dataStore.GetCustomerById(context.Subject.GetSubjectId());
        context.IsActive = customer != null;
    }
}