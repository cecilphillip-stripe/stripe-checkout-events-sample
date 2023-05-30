using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SendGrid.Extensions.DependencyInjection;
using ShipEngineSDK;
using StripeEventsCheckout.ServerlessWorker;
using StripeEventsCheckout.ServerlessWorker.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHttpClient()
            .AddValidatorsFromAssemblyContaining(typeof(CheckoutEventPayload))
            .AddTwilio(ctx.Configuration)
            .AddStripe(ctx.Configuration["Stripe_SecretKey"]);

        services.AddSendGrid(options => { options.ApiKey = ctx.Configuration["SendGrid_ApiKey"]; });
        services.AddTransient<ShipEngine>(_ => new ShipEngine(ctx.Configuration["ShipEngineApiKey"]));
        services.AddTransient<SendGridNotifier>();
        services.AddTransient<TwilioNotifier>();
    })
    .Build();

await host.RunAsync();