using Duende.IdentityServer.Models;
using IdentityModel;

namespace StripeEventsCheckout.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResource(name: "stripe",new[] {"stripe_customer", JwtClaimTypes.Role})
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "stripe.events.web",
                ClientSecrets = { new Secret("morpheus".Sha256()) },
                  
                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "email", "stripe" },
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                
                RedirectUris = { "https://localhost:4242/signin-oidc" },
                BackChannelLogoutUri = "https://localhost:4242/bff/backchannel",
                PostLogoutRedirectUris = { "https://localhost:4242/signout-callback-oidc" }
            },
        };
}