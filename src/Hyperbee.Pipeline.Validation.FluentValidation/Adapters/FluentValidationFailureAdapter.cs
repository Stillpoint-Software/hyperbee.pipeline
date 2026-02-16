using Hyperbee.Pipeline.Validation;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Adapts a FluentValidation <see cref="FluentValidation.Results.ValidationFailure"/> to <see cref="IValidationFailure"/>.
/// </summary>
internal class FluentValidationFailureAdapter : IValidationFailure
{
    private readonly FV.Results.ValidationFailure _inner;

    public FluentValidationFailureAdapter( FV.Results.ValidationFailure failure )
    {
        _inner = failure;
    }

    public string PropertyName => _inner.PropertyName;
    public string ErrorMessage => _inner.ErrorMessage;
    public string? ErrorCode
    {
        get => _inner.ErrorCode;
        set => _inner.ErrorCode = value;
    }
    public object? AttemptedValue => _inner.AttemptedValue;
}
