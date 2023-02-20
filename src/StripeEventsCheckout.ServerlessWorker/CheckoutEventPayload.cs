using FluentValidation;

namespace StripeEventsCheckout.ServerlessWorker;

public record class CheckoutEventPayload(string CheckoutId, string Status, string Event);

public class CheckoutEventPayloadValidator : AbstractValidator<CheckoutEventPayload>
{
    public CheckoutEventPayloadValidator()
    {
        RuleFor(x => x.CheckoutId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Event).NotEmpty();
    }
}