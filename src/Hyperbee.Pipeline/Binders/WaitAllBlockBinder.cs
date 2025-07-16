using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;


internal class WaitAllBlockBinder<TStart, TOutput> : ConditionalBlockBinder<TStart, TOutput>
{
    private MiddlewareAsync<object, object> Middleware { get; }

    public WaitAllBlockBinder( FunctionAsync<TStart, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : this( null, function, middleware, configure )
    {
    }

    public WaitAllBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TStart, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( condition, function, configure )
    {
        Middleware = middleware;
    }

    public FunctionAsync<TStart, TNext> Bind<TNext>( FunctionAsync<TOutput, object>[] nexts, WaitAllReducer<TOutput, TNext> reducer )
    {
        ArgumentNullException.ThrowIfNull( reducer );

        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            // WaitAllBlockBinder is unique in that it is both a block configure and a step.
            // The reducer is the step action, and because it is a step, we need to ensure
            // that middleware is called. Middleware requires us to pass in the execution
            // function that it wraps. This requires an additional level of wrapping.

            return await WaitAllAsync( context, nextArgument, nexts, reducer ).ConfigureAwait( false );
        };
    }

    private async Task<TNext> WaitAllAsync<TNext>( IPipelineContext context, TOutput nextArgument, FunctionAsync<TOutput, object>[] nexts, WaitAllReducer<TOutput, TNext> reducer )
    {
        var contextControl = (IPipelineContextControl) context;
        using var _ = contextControl.CreateFrame( context, Configure, nameof( WaitAllAsync ) );

        var results = new WaitAllResult[nexts.Length];
        var items = nexts.Select( ( x, i ) => new { next = x, index = i } );

        await items.ForEachAsync( async item =>
        {
            var innerContext = context.Clone( false ); // context fork

            var result = await ProcessStatementAsync( item.next, innerContext, nextArgument ).ConfigureAwait( false );

            results[item.index] = new WaitAllResult { Context = innerContext, Result = result };
        } ).ConfigureAwait( false );

        return reducer( context, nextArgument, results );
    }

    private async Task<object> ProcessStatementAsync( FunctionAsync<TOutput, object> next, IPipelineContext context, TOutput nextArgument )
    {
        if ( Middleware == null )
            return await next( context, nextArgument ).ConfigureAwait( false );

        return await Middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await next( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}
