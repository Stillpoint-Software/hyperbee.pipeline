namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Defines a validator for a specific type.
/// </summary>
/// <typeparam name="T">The type to validate. Must be a reference type.</typeparam>
public interface IValidator<in T> where T : class
{
    /// <summary>
    /// Validates the specified instance asynchronously.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the validation operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
    Task<IValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the specified instance asynchronously with custom validation options.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="configure">An action to configure validation options such as RuleSets.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the validation operation.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
    Task<IValidationResult> ValidateAsync(T instance, Action<IValidationContext> configure, CancellationToken cancellationToken = default);
}
