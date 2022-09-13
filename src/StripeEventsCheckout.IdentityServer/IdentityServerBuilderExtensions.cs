using StripeEventsCheckout.IdentityServer.Data;

namespace StripeEventsCheckout.IdentityServer;

public static class IdentityServerBuilderExtensions
{
    public static IIdentityServerBuilder AddMongoServices(this IIdentityServerBuilder builder)
    {
        return builder.AddProfileService<CustomerProfileService>()
            .AddResourceOwnerValidator<CustomResourceOwnerPasswordValidator>();
    }
}