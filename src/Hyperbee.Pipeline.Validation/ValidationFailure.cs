namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Base implementation of <see cref="IValidationFailure"/>.
/// </summary>
/// <remarks>
/// This class provides a concrete implementation of a validation failure that can be used
/// by any validation framework adapter or custom validator implementation.
/// </remarks>
public class ValidationFailure : IValidationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailure"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message describing the validation failure.</param>
    public ValidationFailure( string propertyName, string errorMessage )
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }

    /// <inheritdoc />
    public string PropertyName { get; set; }

    /// <inheritdoc />
    public string ErrorMessage { get; set; }

    /// <inheritdoc />
    public string? ErrorCode { get; set; }

    /// <inheritdoc />
    public object? AttemptedValue { get; set; }

    /// <summary>
    /// Creates a new <see cref="ValidationFailure"/> instance with the specified property name, error message, and optional error code.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message describing the validation failure.</param>
    /// <param name="errorCode">Optional error code to associate with the validation failure.</param>
    /// <returns>A new <see cref="IValidationFailure"/> instance.</returns>
    public static IValidationFailure Create( string propertyName, string errorMessage, string? errorCode = null )
    {
        return new ValidationFailure( propertyName, errorMessage ) { ErrorCode = errorCode };
    }
}
