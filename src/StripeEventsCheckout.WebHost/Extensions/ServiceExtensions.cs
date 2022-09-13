using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Stripe;
using StripeEventsCheckout.WebHost.Data;
using StripeEventsCheckout.WebHost.Models.Config;
using Twilio;
using Twilio.Clients;

namespace StripeEventsCheckout.WebHost.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDbServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDatabaseOptions>(config.GetSection("MongoDatabase"));
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = MongoClientSettings.FromConnectionString(config["MongoDatabase:ConnectionString"]);

            // Setting the version of the Stable API
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            settings.LinqProvider = LinqProvider.V3;
            return new MongoClient(settings);
        });

        services.AddTransient<IEventListingDataStore, MongoDataStore>();

        return services;
    }

    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration config)
    {
        StripeConfiguration.ApiKey = config["SecretKey"];
        services.Configure<StripeOptions>(config);

        var appInfo = new AppInfo
        {
            Name = "StripeEvents",
            Version = "0.1.0"
        };
        StripeConfiguration.AppInfo = appInfo;

        services.AddHttpClient("Stripe");
        services.AddTransient<IStripeClient, StripeClient>(s =>
        {
            var clientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = new SystemNetHttpClient(
                httpClient: clientFactory.CreateClient("Stripe"),
                maxNetworkRetries: StripeConfiguration.MaxNetworkRetries,
                appInfo: appInfo,
                enableTelemetry: StripeConfiguration.EnableTelemetry);

            return new StripeClient(apiKey: StripeConfiguration.ApiKey, httpClient: httpClient);
        });

        return services;
    }

    public static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration config)
    {
        string accountSid = config["AccountSID"];
        string authToken = config["AuthToken"];
        TwilioClient.Init(accountSid, authToken);

        services.Configure<TwilioOptions>(config);
        services.AddHttpClient("Twilio");
        services.AddTransient<ITwilioRestClient, TwilioRestClient>(s =>
        {
            var clientFactory = s.GetRequiredService<IHttpClientFactory>();
            var twilioRestClient = new TwilioRestClient(accountSid, authToken,
                httpClient: new Twilio.Http.SystemNetHttpClient(clientFactory.CreateClient("Twilio")));

            TwilioClient.SetRestClient(twilioRestClient);

            return twilioRestClient;
        });
        return services;
    }

    public static IServiceCollection AddAuthSetup(this IServiceCollection services, IConfiguration config)
    {
        services.AddBff();
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-blazor";
                options.Cookie.SameSite = SameSiteMode.Strict;
            }).AddOpenIdConnect( options =>
            {
                var openIdSettings = config.GetSection("OpenIdConnect");
                options.Authority = openIdSettings.GetValue<string>("Authority");
                
                // confidential client using code flow + PKCE
                options.ClientId = openIdSettings.GetValue<string>("ClientId");
                options.ClientSecret = openIdSettings.GetValue<string>("ClientSecret");
                options.ResponseType = "code";
                options.ResponseMode = "query";

                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                // request scopes + refresh tokens
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
            });

        return services;
    }
}