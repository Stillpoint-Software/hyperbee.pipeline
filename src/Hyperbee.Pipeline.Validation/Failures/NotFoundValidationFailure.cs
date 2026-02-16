using System.Net;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents a validation failure that occurs when a requested resource is not found.
/// </summary>
/// <remarks>This class is used to indicate that a validation error has occurred because a requested
/// entity or resource does not exist. It extends <see cref="ApplicationValidationFailure"/> to provide
/// additional context specific to not found errors.</remarks>
public class NotFoundValidationFailure : ApplicationValidationFailure
{
    public NotFoundValidationFailure( string propertyName, string errorMessage )
        : base( propertyName, errorMessage )
    {
        ErrorCode = nameof( HttpStatusCode.NotFound );
    }
}
