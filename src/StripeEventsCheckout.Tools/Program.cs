using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Stripe;
using System.Reflection;

DotNetEnv.Env.Load();

var builder = BuildCommandLine();
builder.UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureLogging(logbuilder =>
                    {
                        logbuilder.SetMinimumLevel(LogLevel.Error);
                    });
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

    // Status Command
    var statusCommand = new Command("status");
    statusCommand.Handler = CommandHandler.Create<IHost>(StatusHandler);
    root.AddCommand(statusCommand);

    // Setup Command
    var setupCommand = new Command("setup");
    setupCommand.Handler = CommandHandler.Create<IHost>(SetupHandler);
    root.AddCommand(setupCommand);

    return new CommandLineBuilder(root);
}

async Task StatusHandler(IHost host)
{
    var factory = host.Services.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("Stripe");
    try
    {
        var response = await client.GetAsync("https://status.stripe.com/current");
        var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var statusMessage = payload!.RootElement.GetProperty("message").GetString();

        var asm = Assembly.GetEntryAssembly();

        var versionString = asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                .InformationalVersion
                                .ToString();
        var asmName = asm?.GetName().Name;

        var content = new Markup(
            $"[white]Version: {versionString}[/]\n" +
            $"Status: [green]{statusMessage}[/]"
        );

        var panel = new Panel(content)
                .DoubleBorder()
                .Header($"[white]Name: {asmName}[/]")
                .HeaderAlignment(Justify.Center);
        AnsiConsole.Write(panel);

    }
    catch (JsonException)
    {
        AnsiConsole.MarkupLine($"[red]Unable to parse response. Expected JSON[/]");
    }
};

async Task SetupHandler(IHost host)
{
    var stripeClient = host.Services.GetRequiredService<IStripeClient>();
    var prodSvc = new ProductService(stripeClient);
    var priceSvc = new PriceService(stripeClient);

    var data = DemoData.Retrieve();

    foreach (var item in data)
    {
        AnsiConsole.MarkupLine($"[blue]Creating Product {item.Product.Name} - ${item.Product.Price / 100d}[/]");
        var prodCreateOptions = new ProductCreateOptions
        {
            Name = item.Product.Name,
            Images = new List<string> { item.Product.Image },
            Metadata = new Dictionary<string, string>
            {
                ["owner"] = item.Email,
                ["ownerName"] = item.Company
            }
        };

        var newProduct = await prodSvc.CreateAsync(prodCreateOptions);

        var priceCreateOptions = new PriceCreateOptions
        {
            Product = newProduct.Id,
            UnitAmount = item.Product.Price,
            Nickname = item.Product.Name,
            Currency = "usd",
            Metadata = new Dictionary<string, string>
            {
                ["image_url"] = item.Product.Image
            }
        };

        var prodPrice = await priceSvc.CreateAsync(priceCreateOptions);

        AnsiConsole.MarkupLine($"[Green]Created {newProduct.Name} - {newProduct.Id} - ${prodPrice.UnitAmount / 100m}[/] \n");
    }
}
