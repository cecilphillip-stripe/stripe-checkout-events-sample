using Amazon.SQS;
using Serilog;
using StripeEventsCheckout.ApiServer.Workers;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration, "Serilog")
    .CreateLogger();

// Add services to the container.
builder.Host.UseSerilog();
builder.Services.AddStripe(builder.Configuration.GetSection("Stripe"));
builder.Services.AddTransient<IAmazonSQS, AmazonSQSClient>();

builder.Services.AddControllers();

if (builder.Configuration.GetValue<bool>("SeedProductData"))
{
    builder.Services.AddHostedService<SeederWorker>();
}

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");
app.Run();