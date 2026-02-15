using System.Reflection;
using FluentValidation.Results;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for converting <see cref="CommandResult"/> and <see cref="CommandResult{T}"/> instances
/// to <see cref="IResult"/> representations.
/// </summary>
/// <remarks>These extension methods simplify the process of transforming command execution results into
/// standardized <see cref="IResult"/> objects, which can be used in APIs or other result-driven workflows. The methods
/// handle invalid commands by returning appropriate error results, such as forbidden, unauthorized, or bad request
/// responses, based on the validation context.</remarks>
public static class CommandResultExtensions
{
    /// <summary>
    /// Converts a <see cref="CommandResult{T}"/> to an <see cref="IResult"/> representation.
    /// </summary>
    /// <typeparam name="T">The type of the result contained in the <see cref="CommandResult{T}"/>.</typeparam>
    /// <param name="commandResult">The command result to convert. Must not be <see langword="null"/>.</param>
    /// <returns>An <see cref="IResult"/> representing the outcome of the command. If the command is invalid,  returns an error
    /// result; otherwise, returns an <see cref="IResult"/> with the successful result.</returns>
    public static IResult ToResult<T>(this CommandResult<T> commandResult)
    {
        if (TryHandleInvalidCommand(commandResult.Context, commandResult.CommandType, out var errorResult))
            return errorResult;

        return Results.Ok(commandResult.Result);
    }

    /// <summary>
    /// Converts a <see cref="CommandResult{TSource}"/> to an <see cref="IResult"/> by transforming the result
    /// using the provided selector function.
    /// </summary>
    /// <typeparam name="TSource">The type of the result contained in the <see cref="CommandResult{TSource}"/>.</typeparam>
    /// <typeparam name="TResult">The type of the transformed result.</typeparam>
    /// <param name="commandResult">The command result to convert. Must not be <see langword="null"/>.</param>
    /// <param name="selector">A function to transform the result from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.</param>
    /// <returns>An <see cref="IResult"/> representing the outcome of the command. If the command is invalid, returns an error
    /// result; otherwise, returns <see cref="Results.Ok"/> with the transformed result if non-null, or <see cref="Results.NotFound"/> if the result is null.</returns>
    public static IResult ToResult<TSource, TResult>(
        this CommandResult<TSource> commandResult,
        Func<TSource, TResult?> selector
    )
        where TResult : class
    {
        if (TryHandleInvalidCommand(commandResult.Context, commandResult.CommandType, out var errorResult))
            return errorResult;

        var transformedResult = selector(commandResult.Result);

        // If the selector returns an IResult, use it directly
        if (transformedResult is IResult result)
            return result;

        return transformedResult is not null ? Results.Ok(transformedResult) : Results.NotFound();
    }

    /// <summary>
    /// Converts a <see cref="CommandResult"/> to an <see cref="IResult"/> based on the command's execution context and
    /// type.
    /// </summary>
    /// <remarks>This method evaluates the validity of the command using its context and type. If the command
    /// is determined to be invalid, an appropriate error result is returned. Otherwise, a successful result is
    /// returned.</remarks>
    /// <param name="commandResult">The result of a command execution, including its context and type.</param>
    /// <returns>An <see cref="IResult"/> representing the outcome of the command. If the command is invalid, returns an error
    /// result; otherwise, returns a successful result.</returns>
    public static IResult ToResult(this CommandResult commandResult)
    {
        if (TryHandleInvalidCommand(commandResult.Context, commandResult.CommandType, out var errorResult))
            return errorResult;

        return Results.Ok();
    }

    /// <summary>
    /// Converts a <see cref="CommandResult{Stream}"/> to an <see cref="IResult"/> that represents a file response.
    /// </summary>
    /// <param name="commandResult">The command result containing the stream to be returned as a file.</param>
    /// <param name="contentType">The MIME type of the file content. Defaults to <c>"application/json"</c>.</param>
    /// <returns>An <see cref="IResult"/> representing the file response. If the command result is invalid, an error result is
    /// returned.</returns>
    public static IResult ToFileResult(
        this CommandResult<Stream> commandResult,
        string contentType = "application/json"
    )
    {
        if (TryHandleInvalidCommand(commandResult.Context, commandResult.CommandType, out var errorResult))
            return errorResult;

        return Results.File(commandResult.Result, contentType);
    }

    private static bool TryHandleInvalidCommand(
        IPipelineContext context,
        MemberInfo commandType,
        out IResult errorResult
    )
    {
        if (!context.IsValid())
        {
            var failures = context.ValidationFailures();

            if ( TryHandleValidationFailure<NotFoundValidationFailure>( failures, StatusCodes.Status404NotFound, out errorResult ))
                return true;

            if ( TryHandleValidationFailure<ForbiddenValidationFailure>( failures, StatusCodes.Status403Forbidden, out errorResult ))   
                return true;

            if ( TryHandleValidationFailure<UnauthorizedValidationFailure>( failures, StatusCodes.Status401Unauthorized, out errorResult ))
                return true;

            if ( TryHandleValidationFailure<ApplicationValidationFailure>( failures, StatusCodes.Status400BadRequest, out errorResult ))
                return true;

            if ( TryHandleValidationFailure<ApplicationValidationFailure>( failures, StatusCodes.Status400BadRequest, out errorResult ))
                return true;

            if ( TryHandleValidationFailure<ValidationFailure>( failures, StatusCodes.Status400BadRequest, out errorResult ))
                return true;

            // This should never be reached, but just in case
            errorResult = Results.Problem(
                detail: "An unexpected validation error occurred.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Failed"
            );
            return true;
        }

        if (context.IsCanceled && context.CancellationValue is IResult cancellationValue)
        {
            errorResult = cancellationValue;

            context.Logger?.LogDebug(
                "Command [name={Name}, action={Action}, cancellationResult={@CancellationResult}]",
                commandType.Name,
                "Canceled",
                errorResult
            );
            return true;
        }

        if (context.IsError)
            throw new CommandException($"Command {commandType.Name} failed", context.Exception);

        errorResult = null!;
        return false;
    }

    private static bool TryHandleValidationFailure<TValidationFailure>(
        IEnumerable<ValidationFailure> failures,
        int statusCode,
        out IResult errorResult,
        Func<IEnumerable<TValidationFailure>, object>? bodySelector = null
    )
        where TValidationFailure : ValidationFailure
    {
        var matchingFailures = failures.OfType<TValidationFailure>().ToList();

        if (matchingFailures.Count == 0)
        {
            errorResult = null!;
            return false;
        }

        // Determine error body: custom selector or default structure
        var errors =
            bodySelector != null ? bodySelector(matchingFailures) : matchingFailures.Select(CreateErrorObject).ToList();

        errorResult = Results.Problem(
            detail: "One or more validation errors occurred.",
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            extensions: new Dictionary<string, object?> { ["errors"] = errors }
        );

        return true;

        // Local function to create error object

        static Dictionary<string, object> CreateErrorObject(ValidationFailure failure)
        {
            var errorDict = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(failure.PropertyName))
                errorDict["propertyName"] = failure.PropertyName;

            if (!string.IsNullOrWhiteSpace(failure.ErrorMessage))
                errorDict["errorMessage"] = failure.ErrorMessage;

            if (!string.IsNullOrWhiteSpace(failure.ErrorCode))
                errorDict["errorCode"] = failure.ErrorCode;

            if (failure.AttemptedValue != null)
                errorDict["attemptedValue"] = failure.AttemptedValue.ToString() ?? string.Empty;

            return errorDict;
        }
    }
}
