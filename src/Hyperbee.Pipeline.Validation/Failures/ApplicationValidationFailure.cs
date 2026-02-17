namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a general application validation failure.
/// </summary>
/// <remarks>
/// This class encapsulates details about a specific validation error, including the name of the property
/// that caused the failure and the associated error message. It is typically used to report validation issues
/// encountered in an application.
/// </remarks>
/// <param name="propertyName">The name of the property that failed validation.</param>
/// <param name="errorMessage">The error message describing the validation failure.</param>
public class ApplicationValidationFailure( string propertyName, string errorMessage )
    : ValidationFailure( propertyName, errorMessage )
{ }
