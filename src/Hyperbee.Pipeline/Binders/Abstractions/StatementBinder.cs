using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class StatementBinder<TStart, TOutput> : Binder<TStart, TOutput>
{
    protected MiddlewareAsync<object, object> Middleware { get; }

    protected StatementBinder( FunctionAsync<TStart, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, configure )
    {
        Middleware = middleware;
    }

    protected virtual async Task<TNext> ProcessStatementAsync<TNext>( FunctionAsync<TOutput, TNext> nextFunction, IPipelineContext context, TOutput nextArgument, string frameName )
    {
        var contextControl = (IPipelineContextControl) context;

        using var _ = contextControl.CreateFrame( context, Configure, frameName );

        if ( Middleware == null )
            return await nextFunction( context, nextArgument ).ConfigureAwait( false );

        return (TNext) await Middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}
