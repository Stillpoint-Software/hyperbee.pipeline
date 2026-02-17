using Hyperbee.Pipeline.Validation;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Adapts a FluentValidation <see cref="FluentValidation.Results.ValidationResult"/> to <see cref="IValidationResult"/>.
/// </summary>
internal class FluentValidationResultAdapter : IValidationResult
{
    private readonly FV.Results.ValidationResult _inner;

    public FluentValidationResultAdapter( FV.Results.ValidationResult result )
    {
        _inner = result;
    }

    public bool IsValid => _inner.IsValid;

    public IList<IValidationFailure> Errors =>
        _inner.Errors.Select( e => new FluentValidationFailureAdapter( e ) as IValidationFailure ).ToList();
}
