using Microsoft.AspNetCore.Http;

namespace Hyperbee.Pipeline.AspNetCore;

/// <summary>
/// Wraps an <see cref="IResult"/> with an action that decorates the <see cref="HttpContext"/>
/// before the inner result executes. Used by <see cref="ResultExtensions.WithHeader"/> and
/// <see cref="ResultExtensions.WithNoContent"/> to chain response customizations.
/// </summary>
internal sealed class DecoratedResult( IResult inner, Action<HttpContext> decorator ) : IResult
{
    internal IResult Inner { get; } = inner;
    internal Action<HttpContext> Decorator { get; } = decorator;

    public async Task ExecuteAsync( HttpContext httpContext )
    {
        Decorator( httpContext );
        await Inner.ExecuteAsync( httpContext );
    }
}
