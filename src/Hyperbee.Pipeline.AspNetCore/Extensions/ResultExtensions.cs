using Microsoft.AspNetCore.Http;

namespace Hyperbee.Pipeline.AspNetCore.Extensions;

/// <summary>
/// Chainable extension methods for decorating <see cref="IResult"/> responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Adds an HTTP header to the response. If the value is null or whitespace, the
    /// header is not added. Multiple headers can be chained.
    /// </summary>
    /// <param name="result">The result to decorate.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value. If null or whitespace, the result is returned unchanged.</param>
    /// <returns>A decorated <see cref="IResult"/> that sets the header before executing.</returns>
    public static IResult WithHeader( this IResult result, string name, string? value )
    {
        if ( string.IsNullOrWhiteSpace( value ) )
            return result;

        return new DecoratedResult( result, ctx => ctx.Response.Headers[name] = value );
    }

    /// <summary>
    /// Replaces the response with a 204 No Content result while preserving all
    /// previously chained decorators (e.g., headers).
    /// </summary>
    /// <param name="result">The result to replace.</param>
    /// <returns>A 204 No Content <see cref="IResult"/> with all prior decorators preserved.</returns>
    public static IResult WithNoContent( this IResult result )
    {
        if ( result is DecoratedResult decorated )
            return new DecoratedResult( Results.NoContent(), decorated.Decorators );

        return Results.NoContent();
    }
}
