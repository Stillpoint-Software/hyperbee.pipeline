using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WaitAllBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private MiddlewareAsync<object, object> Middleware { get; }
    private Action<IPipelineContext> Configure { get; }
    private Function<TOutput, bool> Condition { get; }

    public WaitAllBlockBinder( FunctionAsync<TInput, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : this( null, function, middleware, configure )
    {
    }

    public WaitAllBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
    {
        Condition = condition;
        Pipeline = function;
        Middleware = middleware;
        Configure = configure;
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, object>[] nexts, WaitAllReducer<TOutput, TNext> reducer )
    {
        ArgumentNullException.ThrowIfNull( reducer );

        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

            if ( Condition != null && !Condition( context, nextArgument ) )
                return (TNext) (object) nextArgument;

            // mind cancellation and execute
            var contextControl = (IPipelineContextControl) context;

            if ( contextControl.HandleCancellationRequested( nextArgument ) )
                return default;

            using ( contextControl.CreateFrame( context, Configure, nameof( WaitAllAsync ) ) )
            {
                return await Next( WaitAllAsync, context, nextArgument ).ConfigureAwait( false );
            }

            // WaitAllBlockBinder is unique in that it is both a block configure and a step.
            // The reducer is the step action, and because it is a step, we need to ensure
            // that middleware is called. Middleware requires us to pass in the execution
            // function that it wraps. This requires an additional level of wrapping.

            async Task<TNext> WaitAllAsync( IPipelineContext context1, TOutput _ )
            {
                var results = new WaitAllResult[nexts.Length];
                var items = nexts.Select( ( x, i ) => new { next = x, index = i } );

                await items.ForEachAsync( async item =>
                    {
                        var innerContext = context1.Clone( false ); // context fork

                        var result = await item.next( innerContext, nextArgument ).ConfigureAwait( false );

                        results[item.index] = new WaitAllResult { Context = innerContext, Result = result };
                    } )
                    .ConfigureAwait( false );

                return reducer( context, nextArgument, results );
            }
        };
    }

    private async Task<TNext> Next<TNext>( FunctionAsync<TOutput, TNext> waitAll, IPipelineContext context, TOutput nextArgument )
    {
        if ( Middleware == null )
            return await waitAll( context, nextArgument ).ConfigureAwait( false );

        return (TNext) await Middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await waitAll( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}
