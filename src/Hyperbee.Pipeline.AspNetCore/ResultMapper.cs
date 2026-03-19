using System.Reflection;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.AspNetCore;

/// <summary>
/// Base class for mapping <see cref="CommandResult"/> instances to <see cref="IResult"/>.
/// Override virtual methods to customize error handling, status codes, and success responses.
/// </summary>
public class ResultMapper
{
    /// <summary>
    /// Default mapper instance used when no custom mapper is provided.
    /// </summary>
    public static ResultMapper Default { get; } = new();

    /// <summary>
    /// Maps an exception to an <see cref="IResult"/>. Return <see langword="null"/> to rethrow
    /// the exception as a <see cref="CommandException"/>.
    /// </summary>
    /// <param name="exception">The exception from the pipeline context.</param>
    /// <returns>An <see cref="IResult"/> for the error, or <see langword="null"/> to rethrow.</returns>
    public virtual IResult? MapException( Exception exception )
    {
        return Results.Problem(
            detail: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: ReasonPhrases.GetReasonPhrase( StatusCodes.Status500InternalServerError )
        );
    }

    /// <summary>
    /// Returns the HTTP status code for a validation failure. Override to customize
    /// status code mapping for specific failure types.
    /// </summary>
    /// <param name="failure">The validation failure.</param>
    /// <returns>The HTTP status code to use.</returns>
    public virtual int GetStatusCode( IValidationFailure failure ) => failure switch
    {
        NotFoundValidationFailure => StatusCodes.Status404NotFound,
        ForbiddenValidationFailure => StatusCodes.Status403Forbidden,
        UnauthorizedValidationFailure => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status422UnprocessableEntity
    };

    /// <summary>
    /// Maps validation failures to an <see cref="IResult"/>. Override to customize
    /// the ProblemDetails structure or error response format.
    /// </summary>
    /// <param name="failures">The validation failures.</param>
    /// <param name="statusCode">The HTTP status code determined by <see cref="GetStatusCode"/>.</param>
    /// <returns>An <see cref="IResult"/> representing the validation error response.</returns>
    public virtual IResult MapValidationFailures( IEnumerable<IValidationFailure> failures, int statusCode )
    {
        var errors = failures.Select( CreateErrorObject ).ToList();

        return Results.Problem(
            detail: "One or more validation errors occurred.",
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase( statusCode ),
            extensions: new Dictionary<string, object?> { ["errors"] = errors }
        );

        static Dictionary<string, object> CreateErrorObject( IValidationFailure failure )
        {
            var errorDict = new Dictionary<string, object>();

            if ( !string.IsNullOrWhiteSpace( failure.PropertyName ) )
                errorDict["propertyName"] = failure.PropertyName;

            if ( !string.IsNullOrWhiteSpace( failure.ErrorMessage ) )
                errorDict["errorMessage"] = failure.ErrorMessage;

            if ( !string.IsNullOrWhiteSpace( failure.ErrorCode ) )
                errorDict["errorCode"] = failure.ErrorCode;

            if ( failure.AttemptedValue != null )
                errorDict["attemptedValue"] = failure.AttemptedValue.ToString() ?? string.Empty;

            return errorDict;
        }
    }

    /// <summary>
    /// Maps a cancellation to an <see cref="IResult"/>. Return <see langword="null"/>
    /// to fall through to success handling.
    /// </summary>
    /// <param name="cancellationValue">The cancellation value from the pipeline context.</param>
    /// <returns>An <see cref="IResult"/> for the cancellation, or <see langword="null"/>.</returns>
    public virtual IResult? MapCancellation( object? cancellationValue )
    {
        return cancellationValue as IResult;
    }

    /// <summary>
    /// Maps a successful result to an <see cref="IResult"/>. Override to customize
    /// the success response format.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result value.</param>
    /// <returns>An <see cref="IResult"/> representing the success response.</returns>
    public virtual IResult MapSuccess<T>( T result )
    {
        return result is null ? Results.NotFound() : Results.Ok( result );
    }

    /// <summary>
    /// Creates a <see cref="ResultMapper"/> from lambda functions. Any lambda not provided
    /// falls back to the default behavior. Useful for one-off customizations without subclassing.
    /// </summary>
    public static ResultMapper Create(
        Func<Exception, IResult?>? mapException = null,
        Func<IValidationFailure, int>? getStatusCode = null,
        Func<IEnumerable<IValidationFailure>, int, IResult>? mapValidationFailures = null,
        Func<object?, IResult?>? mapCancellation = null
    )
    {
        return new LambdaResultMapper( mapException, getStatusCode, mapValidationFailures, mapCancellation );
    }

    /// <summary>
    /// Orchestrates the mapping of a <see cref="CommandResult"/> through the error handling
    /// pipeline, calling the appropriate virtual methods. This method is called by the
    /// <c>ToResult</c> extension methods.
    /// </summary>
    internal IResult HandleCommand( IPipelineContext context, MemberInfo commandType, Func<IResult> onSuccess )
    {
        // 1. Validation failures
        if ( !context.IsValid() )
        {
            var failures = context.ValidationFailures().ToList();

            // Determine status code using priority: most specific wins
            var statusCode = GetPriorityStatusCode( failures );

            return MapValidationFailures( failures, statusCode );
        }

        // 2. Cancellation
        if ( context.IsCanceled )
        {
            var cancellationResult = MapCancellation( context.CancellationValue );

            if ( cancellationResult != null )
            {
                context.Logger?.LogDebug(
                    "Command [name={Name}, action={Action}, cancellationResult={@CancellationResult}]",
                    commandType.Name,
                    "Canceled",
                    cancellationResult
                );
                return cancellationResult;
            }
        }

        // 3. Exceptions
        if ( context.IsError )
        {
            var exceptionResult = MapException( context.Exception );

            if ( exceptionResult is null )
                throw new CommandException( $"Command {commandType.Name} failed", context.Exception );

            return exceptionResult;
        }

        // 4. Success
        return onSuccess();
    }

    private int GetPriorityStatusCode( List<IValidationFailure> failures )
    {
        // Priority order matches the original waterfall: 404 > 403 > 401 > default
        var statusCodes = failures.Select( GetStatusCode ).Distinct().ToList();

        if ( statusCodes.Contains( StatusCodes.Status404NotFound ) )
            return StatusCodes.Status404NotFound;

        if ( statusCodes.Contains( StatusCodes.Status403Forbidden ) )
            return StatusCodes.Status403Forbidden;

        if ( statusCodes.Contains( StatusCodes.Status401Unauthorized ) )
            return StatusCodes.Status401Unauthorized;

        return statusCodes.FirstOrDefault( StatusCodes.Status422UnprocessableEntity );
    }

    private sealed class LambdaResultMapper(
        Func<Exception, IResult?>? mapException,
        Func<IValidationFailure, int>? getStatusCode,
        Func<IEnumerable<IValidationFailure>, int, IResult>? mapValidationFailures,
        Func<object?, IResult?>? mapCancellation
    ) : ResultMapper
    {
        public override IResult? MapException( Exception exception )
            => mapException != null ? mapException( exception ) : base.MapException( exception );

        public override int GetStatusCode( IValidationFailure failure )
            => getStatusCode != null ? getStatusCode( failure ) : base.GetStatusCode( failure );

        public override IResult MapValidationFailures( IEnumerable<IValidationFailure> failures, int statusCode )
            => mapValidationFailures != null ? mapValidationFailures( failures, statusCode ) : base.MapValidationFailures( failures, statusCode );

        public override IResult? MapCancellation( object? cancellationValue )
            => mapCancellation != null ? mapCancellation( cancellationValue ) : base.MapCancellation( cancellationValue );
    }
}
