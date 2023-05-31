using LanguageExt.Common;

namespace StripeEventsCheckout.ServerlessWorker.Workflows;

public readonly record struct ActivityResult<T>(T? Value, ResultState Status, Exception? Error = default)
{
    public bool IsSuccessful => Error is null && Status == ResultState.Success;
    public bool IsFailure => Status == ResultState.Faulted;

    public static implicit operator ActivityResult<T>(T value) => new(value, ResultState.Success);

    public static implicit operator ActivityResult<T>(Result<T> value)
        => value.Match(
            s => new ActivityResult<T>(s, ResultState.Success),
            err => new ActivityResult<T>(default, ResultState.Faulted, err)
        );
}