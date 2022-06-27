using SendGrid.Extensions.DependencyInjection;
using Serilog;
using StripeEventsCheckout.OrderProcessor;
using StripeEventsCheckout.OrderProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration, "Serilog")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddStripe(builder.Configuration.GetSection("Stripe"));
builder.Services.AddTwilio(builder.Configuration.GetSection("Twilio"));
builder.Services.AddSendGrid(options =>
{
    options.ApiKey = builder.Configuration["SendGrid:APIKEY"];
});
builder.Services.AddControllers();

builder.Services.AddTransient<IMessageSender, SendGridMessageSender>();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.MapControllers();
app.Run();
