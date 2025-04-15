using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class StatementBinder<TInput, TOutput> : Binder<TInput, TOutput>
{
    protected MiddlewareAsync<object, object> Middleware { get; }

    protected StatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
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
