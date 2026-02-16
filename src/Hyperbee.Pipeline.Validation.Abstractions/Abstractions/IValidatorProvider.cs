namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides access to validators for specific types.
/// </summary>
/// <remarks>
/// This interface is used to retrieve validators from a dependency injection container
/// or other validator registration mechanism.
/// </remarks>
public interface IValidatorProvider
{
    /// <summary>
    /// Retrieves a validator for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get a validator for. Must be a reference type.</typeparam>
    /// <returns>
    /// An instance of <see cref="IValidator{T}"/> for the specified type,
    /// or <see langword="null"/> if no validator is registered.
    /// </returns>
    IValidator<T>? For<T>() where T : class;
}
