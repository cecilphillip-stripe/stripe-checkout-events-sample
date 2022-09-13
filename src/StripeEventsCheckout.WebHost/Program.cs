using Microsoft.AspNetCore.CookiePolicy;
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
builder.Services.AddAuthSetup(builder.Configuration);

var notifyBy = builder.Configuration.GetValue<string>("NotifierService");
if ( notifyBy.Equals("twilio", StringComparison.InvariantCultureIgnoreCase))
{
    builder.Services.AddTwilio(builder.Configuration.GetSection("Twilio"));
    builder.Services.AddTransient<IMessageSender, TwilioMessageSender>();
}
else if( notifyBy.Equals("sendgrid", StringComparison.InvariantCultureIgnoreCase))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = builder.Configuration["SendGrid:ApiKey"];
    });
    builder.Services.AddTransient<IMessageSender, SendGridMessageSender>();
}
else
{
    //TODO: No Op?
}

builder.Services.Configure<CookiePolicyOptions>(options => {
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

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

app.UseHttpsRedirection();
app.UseCookiePolicy();

app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();
app.MapControllers();

app.MapFallbackToFile("index.html");
app.Run();