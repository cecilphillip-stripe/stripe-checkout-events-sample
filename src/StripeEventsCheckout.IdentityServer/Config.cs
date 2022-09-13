using Duende.IdentityServer.Models;

namespace StripeEventsCheckout.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
        };

    // public static IEnumerable<ApiScope> ApiScopes =>
    //     new ApiScope[]
    //     {
    //         new ApiScope("api", new[] { "name" })
    //     };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "stripe.events.web",
                ClientSecrets = { new Secret("morphes".Sha256()) },
                  
                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile" },
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,

                RedirectUris = { "https://localhost:4242/signin-oidc" },
                    
                //FrontChannelLogoutUri = "https://localhost:5002/signout-oidc",
                BackChannelLogoutUri = "https://localhost:4242/bff/backchannel",
                PostLogoutRedirectUris = { "https://localhost:4242/signout-callback-oidc" }
            },
        };
}