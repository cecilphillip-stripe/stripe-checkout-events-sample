using FluentValidation;
using Microsoft.Extensions.Hosting;
using StripeEventsCheckout.ServerlessWorker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddValidatorsFromAssemblyContaining(typeof(CheckoutEventPayload));
    })
    .Build();

await host.RunAsync();