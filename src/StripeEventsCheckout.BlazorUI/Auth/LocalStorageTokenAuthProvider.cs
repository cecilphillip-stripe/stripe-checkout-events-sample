using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace StripeEventsCheckout.BlazorUI.Auth;

public class LocalStorageTokenAuthProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private const string AUTH_TYPE = "JWT";

    public LocalStorageTokenAuthProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrEmpty(token))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secureToken = tokenHandler.ReadJwtToken(token);
            identity = new ClaimsIdentity(secureToken.Claims, AUTH_TYPE);
        }

        var userPrincipal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(userPrincipal);

        NotifyAuthenticationStateChanged(Task.FromResult(authState));
        return authState;
    }
}