using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides access to validators for specific plugin types.
/// </summary>
/// <remarks>This class resolves validators for plugin types using a dependency injection container. It is
/// designed to retrieve an implementation of <see cref="IValidator{T}"/> for a given plugin type.</remarks>
public class ValidatorProvider : IValidatorProvider
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorProvider"/> class with the specified dependency injection
    /// container.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection container used to resolve validator instances. Cannot be <see langword="null"/>.</param>
    public ValidatorProvider( IServiceProvider serviceProvider )
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Retrieves a validator for the specified plugin type.
    /// </summary>
    /// <remarks>Use this method to obtain a validator for a specific plugin type, enabling validation logic
    /// to be applied  to instances of that type. Ensure that the requested plugin type has a corresponding validator
    /// registered  in the container.</remarks>
    /// <typeparam name="TPlugin">The type of the plugin for which the validator is requested.  Must be a reference type.</typeparam>
    /// <returns>An instance of <see cref="IValidator{TPlugin}"/> if a validator is registered for the specified plugin type;
    /// otherwise, <see langword="null"/>.</returns>
    public IValidator<TPlugin>? For<TPlugin>()
        where TPlugin : class
    {
        return _serviceProvider.GetService<IValidator<TPlugin>>();
    }
}
