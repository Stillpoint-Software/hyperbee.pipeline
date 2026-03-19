using Microsoft.AspNetCore.Http;

namespace Hyperbee.Pipeline.AspNetCore;

/// <summary>
/// Wraps an <see cref="IResult"/> with a list of actions that decorate the <see cref="HttpContext"/>
/// before the inner result executes. Supports composing multiple decorators via chaining.
/// </summary>
internal sealed class DecoratedResult : IResult
{
    internal IResult Inner { get; }
    internal List<Action<HttpContext>> Decorators { get; }

    internal DecoratedResult( IResult inner, Action<HttpContext> decorator )
    {
        // Flatten: if inner is already a DecoratedResult, absorb its decorators
        if ( inner is DecoratedResult existing )
        {
            Inner = existing.Inner;
            Decorators = [.. existing.Decorators, decorator];
        }
        else
        {
            Inner = inner;
            Decorators = [decorator];
        }
    }

    internal DecoratedResult( IResult inner, List<Action<HttpContext>> decorators )
    {
        Inner = inner;
        Decorators = decorators;
    }

    public async Task ExecuteAsync( HttpContext httpContext )
    {
        foreach ( var decorator in Decorators )
            decorator( httpContext );

        await Inner.ExecuteAsync( httpContext );
    }
}
