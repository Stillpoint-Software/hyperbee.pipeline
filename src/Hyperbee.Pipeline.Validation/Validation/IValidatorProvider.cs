using FluentValidation;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Defines a provider for retrieving validators for specific plugin types.
/// </summary>
/// <remarks>This interface is typically used to obtain a validator for a given plugin type.  The returned
/// validator can be used to validate instances of the specified type.</remarks>
public interface IValidatorProvider
{
    /// <summary>
    /// Retrieves a validator for the specified plugin type.
    /// </summary>
    /// <typeparam name="TPlugin">The type of the plugin to validate. Must be a reference type.</typeparam>
    /// <returns>An instance of <see cref="IValidator{TPlugin}"/> for the specified plugin type, or <see langword="null"/> if no
    /// validator is available.</returns>
    IValidator<TPlugin>? For<TPlugin>()
        where TPlugin : class;
}
