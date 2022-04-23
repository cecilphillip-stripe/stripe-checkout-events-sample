using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Stripe;

DotNetEnv.Env.Load();

var builder = BuildCommandLine();
builder.UseHost(_ => Host.CreateDefaultBuilder(), host =>
                {
                    host.ConfigureServices(services =>
                    {
                        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE__SECRET_KEY");

                        var appInfo = new AppInfo
                        {
                            Name = "StripeEvents.Tools",
                            Version = "0.1.0"
                        };
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
                    });
                });

builder.UseDefaults();
var cmdparser = builder.Build();
await cmdparser.InvokeAsync(args);

CommandLineBuilder BuildCommandLine()
{
    var root = new RootCommand();
    var statusCommand = new Command("status");
    statusCommand.Handler = CommandHandler.Create<IHost>(StatusHandler);
    root.AddCommand(statusCommand);
    return new CommandLineBuilder(root);
}



async Task StatusHandler(IHost host)
{
    var factory = host.Services.GetRequiredService<IHttpClientFactory>();


    var client = factory.CreateClient("Stripe");
    var response = await client.GetAsync("https://status.stripe.com/current");
    var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
    var statusMessage = payload.RootElement.GetProperty("message").GetString();
    AnsiConsole.MarkupLine($"[green]{statusMessage}![/]");
};
