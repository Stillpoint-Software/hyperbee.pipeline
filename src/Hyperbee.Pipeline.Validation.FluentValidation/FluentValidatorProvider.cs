using Hyperbee.Pipeline.Validation;
using Microsoft.Extensions.DependencyInjection;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Provides access to FluentValidation validators registered in the dependency injection container.
/// </summary>
/// <remarks>
/// This provider resolves FluentValidation validators from the service provider and adapts them
/// to the <see cref="IValidator{T}"/> interface used by Hyperbee.Pipeline.Validation.
/// </remarks>
public class FluentValidatorProvider : IValidatorProvider
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidatorProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection container used to resolve validator instances.</param>
    public FluentValidatorProvider( IServiceProvider serviceProvider )
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IValidator<T>? For<T>() where T : class
    {
        var fluentValidator = _serviceProvider.GetService<FV.IValidator<T>>();
        return fluentValidator != null
            ? new FluentValidatorAdapter<T>( fluentValidator )
            : null;
    }
}
