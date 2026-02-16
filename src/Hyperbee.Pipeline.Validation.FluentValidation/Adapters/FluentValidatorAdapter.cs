using Hyperbee.Pipeline.Validation;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Adapts a FluentValidation <see cref="FluentValidation.IValidator{T}"/> to <see cref="IValidator{T}"/>.
/// </summary>
public class FluentValidatorAdapter<T> : IValidator<T> where T : class
{
    private readonly FV.IValidator<T> _inner;

    public FluentValidatorAdapter( FV.IValidator<T> validator )
    {
        _inner = validator;
    }

    public async Task<IValidationResult> ValidateAsync( T instance, CancellationToken cancellationToken = default )
    {
        var result = await _inner.ValidateAsync( instance, cancellationToken );
        return new FluentValidationResultAdapter( result );
    }

    public async Task<IValidationResult> ValidateAsync(
        T instance,
        Action<IValidationContext> configure,
        CancellationToken cancellationToken = default )
    {
        var adapter = new FluentValidationContextAdapter<T>( instance );
        configure( adapter );

        var context = adapter.CreateContext();
        var result = await _inner.ValidateAsync( context, cancellationToken );
        return new FluentValidationResultAdapter( result );
    }
}
