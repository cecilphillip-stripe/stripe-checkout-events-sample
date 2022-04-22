using Serilog;
using StripeEventsCheckout.ApiServer.Services;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration, "Serilog")
    .CreateLogger();

// Add services to the container.
builder.Host.UseSerilog();
builder.Services.AddStripe(builder.Configuration.GetSection("STRIPE"));
builder.Services.AddTwilio(builder.Configuration.GetSection("TWILIO"));
builder.Services.AddTransient<IMessageSender, TwilioMessageSender>();
builder.Services.AddControllers();

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