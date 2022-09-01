using SendGrid.Extensions.DependencyInjection;
using Serilog;
using StripeEventsCheckout.WebHost.Extensions;
using StripeEventsCheckout.WebHost.Services;
using StripeEventsCheckout.WebHost.Workers;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration, "Serilog")
    .CreateLogger();

// Add services to the container.
builder.Host.UseSerilog();
builder.Services.AddStripe(builder.Configuration.GetSection("Stripe"));

// Add auth services
builder.Services.AddAuthSetup(builder.Configuration.GetSection("JWTSettings"));

if ( builder.Configuration.GetValue<string>("NotifierService", "sendgrid").ToLower() == "twilio")
{
    builder.Services.AddTwilio(builder.Configuration.GetSection("Twilio"));
    builder.Services.AddTransient<IMessageSender, TwilioMessageSender>();
}
else
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = builder.Configuration["SendGrid:ApiKey"];
    });
    builder.Services.AddTransient<IMessageSender, SendGridMessageSender>();
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");
app.Run();