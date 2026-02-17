using System.Net;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a validation failure that occurs when an operation is unauthorized.
/// </summary>
/// <remarks>This class is used to indicate that a validation error has occurred due to an unauthorized operation or
/// access restriction. It extends <see cref="ApplicationValidationFailure"/> to provide additional context specific to
/// authorization errors.</remarks>
public class UnauthorizedValidationFailure : ApplicationValidationFailure
{
    public UnauthorizedValidationFailure( string propertyName, string errorMessage )
        : base( propertyName, errorMessage )
    {
        ErrorCode = nameof( HttpStatusCode.Unauthorized );
    }
}
