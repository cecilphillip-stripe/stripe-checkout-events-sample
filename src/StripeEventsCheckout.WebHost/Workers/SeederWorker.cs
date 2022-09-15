using Spectre.Console;
using Stripe;

namespace StripeEventsCheckout.WebHost.Workers;

public class SeederWorker : BackgroundService
{
    private readonly IStripeClient _stripeClient;
    private readonly ILogger<SeederWorker> _logger;

    public SeederWorker(IStripeClient stripeClient, ILogger<SeederWorker> logger)
    {
        _stripeClient = stripeClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var prodSvc = new ProductService(_stripeClient);
        if (!await AnyExistingProducts(prodSvc, stoppingToken))
        {
            // no existing products. populate stripe account
            _logger.LogInformation("Started Data Seeding...");
            var priceSvc = new PriceService(_stripeClient);

            // Add one-time payment products
            var products = DemoData.RetrieveProducts();
            foreach (var item in products)
            {
                AnsiConsole.MarkupLine($"[blue]Creating Product {item.Product.Name} - ${item.Product.Price / 100d}[/]");
                var prodCreateOptions = new ProductCreateOptions
                {
                    Name = item.Product.Name,
                    Images = new List<string> { item.Product.Image },
                    Metadata = new Dictionary<string, string>
                    {
                        ["owner"] = $"{item.FirstName} {item.LastName}",
                        ["ownerName"] = item.Company
                    }
                };

                var newProduct = await prodSvc.CreateAsync(prodCreateOptions, cancellationToken: stoppingToken);

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

                var prodPrice = await priceSvc.CreateAsync(priceCreateOptions, cancellationToken: stoppingToken);
                AnsiConsole.MarkupLine(
                    $"[Green]Created {newProduct.Name} - {newProduct.Id} - ${prodPrice.UnitAmount / 100m}[/] \n");
            }

            // Add subscription based products
            _logger.LogInformation("Creating subscriptions...");
            var subscriptions = DemoData.RetrieveSubscriptions();
            foreach (var sub in subscriptions)
            {
                AnsiConsole.MarkupLine($"[blue]Creating Subscription Product {sub.Name} [/]");
                var prodCreateOptions = new ProductCreateOptions
                {
                    Name = sub.Name,
                    Images = new List<string> { sub.Image },
                    Metadata = new Dictionary<string, string>
                    {
                        ["owner"] = "C.L.P",
                        ["ownerName"] = "Cecil's Event Experiences"
                    }
                };

                var newProduct = await prodSvc.CreateAsync(prodCreateOptions, cancellationToken: stoppingToken);
                
                // Create monthly and yearly subscriptions. Expects 2 prices per product
                for (var i = 0; i < sub.Prices.Length; i++)
                {
                    var priceCreateOptions = new PriceCreateOptions
                    {
                        Product = newProduct.Id,
                        UnitAmount = sub.Prices[i],
                        Nickname = sub.Name,
                        Currency = "usd",
                        Recurring = new()
                        {
                            Interval = i != 1? "month" : "year",
                            IntervalCount = 1,
                            TrialPeriodDays = 60
                        }
                    };

                    var prodPrice = await priceSvc.CreateAsync(priceCreateOptions, cancellationToken: stoppingToken);
                    AnsiConsole.MarkupLine(
                        $"[Green]Created {newProduct.Name} - {newProduct.Id} - ${prodPrice.UnitAmount / 100m}[/] \n");
                }
            }
            _logger.LogInformation("Data Seeding Completed");
        }
        else
        {
            _logger.LogInformation("There are existing products registered in this Stripe account");
        }
    }

    async Task<bool> AnyExistingProducts(ProductService productService, CancellationToken stoppingToken)
    {
        var listOptions = new ProductListOptions
        {
            Active = true,
            Limit = 1
        };

        var existingProducts = await productService.ListAsync(listOptions, cancellationToken: stoppingToken);
        return existingProducts.Any();
    }
}

public static class DemoData
{
    public static IEnumerable<DemoRecord> RetrieveProducts()
    {
        var data = new DemoRecord[]
        {
            new("Stephen", "Holmes", "FoodieLand Night Market", 
                new("FoodieLand Night Market - Berkeley | October 8-10",
                    5500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F137700905%2F285623250502%2F1%2Foriginal.20210604-004626?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=cea8d8fa42dee21c5740c5de763915a4"
                )
            ),

            new("Rachel", "Wilkins", "Craft Hospitality", 
                new("San Francisco Coffee Festival 2021",
                    6900,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F108330301%2F35694333470%2F1%2Foriginal.20200811-200320?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C150%2C1348%2C674&s=c5f8ca6e7bd6900fae94dbc098b932a7"
                )
            ),

            new("Angela", "Bruton", "Shipyard Trust for the Arts", 
                new("Shipyard Open Studios 2021",
                    1400,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F141827421%2F165638753394%2F1%2Foriginal.20210716-073309?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C22%2C1920%2C960&s=13b4f039611eb487131e34d027cc8a5c"
                )
            ),

            new("Jane", "Diaz", "Young Art Records", 
                new("TOKiMONSTA presented by Young Art Records",
                    3500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F144134661%2F481588047555%2F1%2Foriginal.20210810-174543?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C60%2C1920%2C960&s=54e54d81acde2a8b723576f016fc19ef"
                )
            ),

            new("Paul", "Elliot", "Holly Shaw and the Performers & Creators Lab", 
                new("The Comedy Edge: Stand-Up on the Waterfront",
                    2000,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F116415537%2F38806056114%2F1%2Foriginal.20201031-204217?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=1ab11e65f9bf740b9411d67ccdde50ad"
                )
            ),


            new("Curtis", "Hall", "District Six San Francisco", 
                new("The Night Market",
                    2500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F143639347%2F171723859036%2F1%2Foriginal.20210805-003841?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=178%2C22%2C1840%2C920&s=24792d3688d29247609481307ac2049d"
                )
            ),

            new("Arthur", "Jenkins", "Nor Cal Ski and Snowboard Festivals", 
                new("2021 San Francisco Ski & Snowboard Festival",
                    5000,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F145722117%2F23330292812%2F1%2Foriginal.20210826-185332?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C0%2C2160%2C1080&s=fb620a4c45c97a8a29830e8f8c356815"
                )
            ),

            new("Sally", "Rock", "Noise Pop", 
                new(
                    "Noise Pop 20th Street Block Party",
                    500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F159520329%2F578928699583%2F1%2Foriginal.20211001-140233?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=0%2C25%2C1500%2C750&s=4c9f891cd9a66959bc18b523f98b2815"
                )
            ),

            new("Jenny", "Fields", "Sundaze San Francisco",
                new(
                    "Sundaze Brunch & Marketplace",
                    17500,
                    "https://img.evbuc.com/https%3A%2F%2Fcdn.evbuc.com%2Fimages%2F143641099%2F171723859036%2F1%2Foriginal.20210805-010723?w=800&auto=format%2Ccompress&q=75&sharp=10&rect=61%2C0%2C2004%2C1002&s=58fcb259bb043d8239cc0c14aa04f89d"
                )
            )
        };
        return data;
    }

    public static IEnumerable<DemoSubscription> RetrieveSubscriptions()
    {
        var data = new DemoSubscription[]
        {
            new("Events Explorer Package",
                new long[] { 20000, 220000 },
                "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=2346&q=80"),
            
            new("Events Voyager Package",
                new long[] { 35000, 385000 },
                "https://images.unsplash.com/photo-1549937917-03ccda498729?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1364&q=80"),
            
            new("Events Expedition Package",
                new long[] { 50000, 550000 },
                "https://images.unsplash.com/photo-1575570724505-bf19736b7f7c?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=2340&q=80")
        };
        return data;
    }
}

public record DemoRecord(
    string FirstName,
    string LastName,
    string Company,
    DemoProduct Product
);

public record DemoProduct(string Name, long Price, string Image);

public record DemoSubscription(string Name, long[] Prices, string Image);