using System.Net;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a validation failure that occurs when an operation is forbidden.
/// </summary>
/// <remarks>This class is used to indicate that a validation error has occurred due to a forbidden operation or
/// access restriction. It extends <see cref="ApplicationValidationFailure"/> to provide additional context specific to
/// forbidden errors.</remarks>
public class ForbiddenValidationFailure : ApplicationValidationFailure
{
    public ForbiddenValidationFailure( string propertyName, string errorMessage )
        : base( propertyName, errorMessage )
    {
        ErrorCode = nameof( HttpStatusCode.Forbidden );
    }
}
