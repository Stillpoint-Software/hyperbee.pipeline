using Hyperbee.Pipeline.Commands;
using Microsoft.AspNetCore.Http;

namespace Hyperbee.Pipeline.AspNetCore.Extensions;

/// <summary>
/// Extension methods for converting <see cref="CommandResult"/> and <see cref="CommandResult{T}"/>
/// to <see cref="IResult"/> using the <see cref="ResultMapper"/> pipeline.
/// </summary>
public static class CommandResultExtensions
{
    // Generic overloads

    /// <summary>
    /// Converts a <see cref="CommandResult{T}"/> to an <see cref="IResult"/>.
    /// </summary>
    public static IResult ToResult<T>( this CommandResult<T> commandResult, ResultMapper? mapper = null )
    {
        mapper ??= ResultMapper.Default;

        return mapper.HandleCommand(
            commandResult.Context,
            commandResult.CommandType,
            () => mapper.MapSuccess( commandResult.Result )
        );
    }

    /// <summary>
    /// Converts a <see cref="CommandResult{T}"/> to an <see cref="IResult"/> using a
    /// result mapper function for custom error and success handling.
    /// </summary>
    /// <remarks>
    /// The <paramref name="resultMapper"/> runs before all other handling. Return a non-null
    /// <see cref="IResult"/> to short-circuit, or <see langword="null"/> to fall through
    /// to the <see cref="ResultMapper"/> pipeline.
    /// </remarks>
    public static IResult ToResult<T>(
        this CommandResult<T> commandResult,
        Func<CommandResult<T>, IResult?> resultMapper,
        ResultMapper? mapper = null
    )
    {
        var mapped = resultMapper( commandResult );
        if ( mapped != null )
            return mapped;

        return commandResult.ToResult( mapper );
    }

    /// <summary>
    /// Converts a <see cref="CommandResult{TSource}"/> to an <see cref="IResult"/> using a
    /// content selector to project the result body.
    /// </summary>
    /// <typeparam name="TSource">The type of the command result.</typeparam>
    /// <typeparam name="TResult">The projected body type.</typeparam>
    /// <param name="commandResult">The command result to convert.</param>
    /// <param name="contentSelector">Projects the result into the response body shape.</param>
    /// <param name="mapper">Optional custom mapper. Uses <see cref="ResultMapper.Default"/> if null.</param>
    public static IResult ToResult<TSource, TResult>(
        this CommandResult<TSource> commandResult,
        Func<TSource, TResult?> contentSelector,
        ResultMapper? mapper = null
    )
        where TResult : class
    {
        mapper ??= ResultMapper.Default;

        return mapper.HandleCommand(
            commandResult.Context,
            commandResult.CommandType,
            () =>
            {
                var transformed = contentSelector( commandResult.Result );

                if ( transformed is IResult result )
                    return result;

                return mapper.MapSuccess( transformed );
            }
        );
    }

    // Non-generic overloads

    /// <summary>
    /// Converts a <see cref="CommandResult"/> to an <see cref="IResult"/>.
    /// </summary>
    public static IResult ToResult( this CommandResult commandResult, ResultMapper? mapper = null )
    {
        mapper ??= ResultMapper.Default;

        return mapper.HandleCommand(
            commandResult.Context,
            commandResult.CommandType,
            mapper.MapSuccess
        );
    }

    /// <summary>
    /// Converts a <see cref="CommandResult"/> to an <see cref="IResult"/> using a
    /// result mapper function for custom error and success handling.
    /// </summary>
    public static IResult ToResult(
        this CommandResult commandResult,
        Func<CommandResult, IResult?> resultMapper,
        ResultMapper? mapper = null
    )
    {
        var mapped = resultMapper( commandResult );
        if ( mapped != null )
            return mapped;

        return commandResult.ToResult( mapper );
    }

    // File result

    /// <summary>
    /// Converts a <see cref="CommandResult{Stream}"/> to a file <see cref="IResult"/>.
    /// </summary>
    public static IResult ToFileResult(
        this CommandResult<Stream> commandResult,
        string contentType = "application/json",
        ResultMapper? mapper = null
    )
    {
        mapper ??= ResultMapper.Default;

        return mapper.HandleCommand(
            commandResult.Context,
            commandResult.CommandType,
            () => Results.File( commandResult.Result, contentType )
        );
    }
}
