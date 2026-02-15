using FluentValidation.Results;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a validation failure.
/// </summary>
/// <remarks>This class encapsulates details about a specific validation error, including the name of the property
/// that caused the failure and the associated error message. It is typically used to report validation issues
/// encountered in an application.</remarks>
/// <param name="propertyName"></param>
/// <param name="errorMessage"></param>
public class ApplicationValidationFailure( string propertyName, string errorMessage )
    : ValidationFailure( propertyName, errorMessage, null )
{ }
