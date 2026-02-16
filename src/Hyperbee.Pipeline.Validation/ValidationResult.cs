namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Base implementation of <see cref="IValidationResult"/>.
/// </summary>
/// <remarks>
/// This class provides a concrete implementation of a validation result that can be used
/// by any validation framework adapter or custom validator implementation.
/// </remarks>
public class ValidationResult : IValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class with no failures (valid).
    /// </summary>
    public ValidationResult()
    {
        Errors = new List<IValidationFailure>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class with the specified failures.
    /// </summary>
    /// <param name="failures">The validation failures.</param>
    public ValidationResult( IEnumerable<IValidationFailure> failures )
    {
        Errors = new List<IValidationFailure>( failures );
    }

    /// <inheritdoc />
    public bool IsValid => Errors.Count == 0;

    /// <inheritdoc />
    public IList<IValidationFailure> Errors { get; }
}
