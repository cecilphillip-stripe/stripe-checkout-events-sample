using Microsoft.AspNetCore.CookiePolicy;
using Serilog;
using StripeEventsCheckout.WebHost.Extensions;
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

builder.Services.Configure<CookiePolicyOptions>(options => {
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddControllers();
builder.Services.AddServiceBus(builder.Configuration.GetSection("ServiceBus"));

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