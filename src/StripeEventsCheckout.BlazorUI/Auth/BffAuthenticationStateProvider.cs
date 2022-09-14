using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Components.Authorization;

namespace StripeEventsCheckout.BlazorUI.Auth;

public class BffAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _client;
    private readonly ILogger<BffAuthenticationStateProvider> _logger;

    private PeriodicTimer? _periodicTimer;
    private static readonly TimeSpan UserCacheRefreshInterval = TimeSpan.FromSeconds(60);
    private DateTimeOffset _userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
    private ClaimsPrincipal _cachedUser = new(new ClaimsIdentity());

    public BffAuthenticationStateProvider(IHttpClientFactory clientFactory,
        ILogger<BffAuthenticationStateProvider> logger)
    {
        _client = clientFactory.CreateClient("backend");
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await GetUser();
        var state = new AuthenticationState(user);

        if (user!.Identity!.IsAuthenticated)
        {
            _logger.LogInformation("starting background check..");
            _periodicTimer = new(TimeSpan.FromSeconds(20));
            _ = CheckUserStatus();
        }
        
        return state;
    }

    private async Task CheckUserStatus()
    {
        while (await _periodicTimer!.WaitForNextTickAsync())
        {
            var currentUser = await GetUser(false);
            if (currentUser!.Identity!.IsAuthenticated == false)
            {
                _logger.LogInformation("user logged out");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(currentUser)));
                 _periodicTimer.Dispose();
                 _periodicTimer = null;
            }
        }
    }

    private async ValueTask<ClaimsPrincipal> GetUser(bool useCache = true)
    {
        var now = DateTimeOffset.Now;
        if (useCache && now < _userLastCheck + UserCacheRefreshInterval)
        {
            _logger.LogDebug("Taking user from cache");
            return _cachedUser;
        }

        _logger.LogDebug("Fetching user");
        _cachedUser = await FetchUser();
        _userLastCheck = now;

        return _cachedUser;
    }

    private async Task<ClaimsPrincipal> FetchUser()
    {
        try
        {
            _logger.LogInformation("Fetching user information");
            var response = await _client.GetAsync("bff/user?slide=false");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var claims = await response.Content.ReadFromJsonAsync<List<ClaimRecord>>();

                var identity = new ClaimsIdentity(nameof(BffAuthenticationStateProvider),
                    JwtClaimTypes.Name, JwtClaimTypes.Role);

                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        identity.AddClaim(new Claim(claim.Type, claim.Value.ToString() ?? "no value"));
                    }
                }

                return new ClaimsPrincipal(identity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fetching user failed");
        }

        return new ClaimsPrincipal(new ClaimsIdentity());
    }
    
    record ClaimRecord(string Type, object Value);
}