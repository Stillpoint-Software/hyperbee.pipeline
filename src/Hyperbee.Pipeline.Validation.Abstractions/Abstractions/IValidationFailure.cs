namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a validation failure for a specific property or field.
/// </summary>
public interface IValidationFailure
{
    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets the error message describing the validation failure.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Gets or sets the error code associated with the validation failure.
    /// </summary>
    string? ErrorCode { get; set; }

    /// <summary>
    /// Gets the value that was attempted to be set on the property when validation failed.
    /// </summary>
    object? AttemptedValue { get; }
}
