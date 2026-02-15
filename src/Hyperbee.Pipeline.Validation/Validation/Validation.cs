using FluentValidation.Results;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Helper method for creating validation failures.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Creates a <see cref="ValidationFailure"/> with the specified property name, error message, and optional error code.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message describing the validation failure.</param>
    /// <param name="errorCode">An optional error code to associate with the validation failure.</param>
    /// <returns>A new <see cref="ValidationFailure"/> instance.</returns>
    public static ValidationFailure Failure( string propertyName, string errorMessage, string? errorCode = null )
    {
        return new( propertyName, errorMessage ) { ErrorCode = errorCode };
    }
}
