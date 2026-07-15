namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// The exception thrown by <see cref="PipelineValidationExtensions.ThrowIfInvalid"/> when a
/// pipeline context contains validation failures.
/// </summary>
/// <remarks>
/// Carries the <see cref="IValidationResult"/> so callers can inspect the individual
/// <see cref="IValidationFailure"/> entries, including <see cref="IValidationFailure.ErrorCode"/>.
/// </remarks>
[Serializable]
public class PipelineValidationException : Exception
{
    /// <summary>
    /// The validation result containing the failures that caused the exception.
    /// </summary>
    public IValidationResult ValidationResult { get; }

    /// <summary>
    /// Initializes a new instance with a message composed from the failure messages.
    /// </summary>
    /// <param name="validationResult">The validation result containing the failures.</param>
    public PipelineValidationException( IValidationResult validationResult )
        : this( CreateMessage( validationResult ), validationResult )
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="validationResult">The validation result containing the failures.</param>
    public PipelineValidationException( string message, IValidationResult validationResult )
        : base( message )
    {
        ArgumentNullException.ThrowIfNull( validationResult );

        ValidationResult = validationResult;
    }

    private static string CreateMessage( IValidationResult validationResult )
    {
        ArgumentNullException.ThrowIfNull( validationResult );

        return $"Validation failed: {string.Join( "; ", validationResult.Errors.Select( failure => failure.ErrorMessage ) )}";
    }
}
