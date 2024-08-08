using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WaitAllBlockBinder<TInput, TOutput>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    private Expression<MiddlewareAsync<object, object>> Middleware { get; }
    private Expression<Action<IPipelineContext>> Configure { get; }
    private Expression<Function<TOutput, bool>> Condition { get; }

    public WaitAllBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : this( null, function, middleware, configure )
    {
    }

    public WaitAllBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
    {
        Condition = condition;
        Pipeline = function;
        Middleware = middleware;
        Configure = configure;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind<TNext>( Expression<FunctionAsync<TInput, TOutput>[]> next, WaitAllReducer<TOutput, TNext> reducer )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( WaitAllBlockBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ), typeof( TNext ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            next,
            Pipeline,
            Middleware,
            Configure,
            Condition,
            Expression.Constant( reducer )
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    public static FunctionAsync<TInput, TNext> BindImpl<TNext>( 
        FunctionAsync<TOutput, object>[] nexts, 
        FunctionAsync<TInput, TOutput> pipeline, 
        MiddlewareAsync<object, object> middleware, 
        Action<IPipelineContext> configure,
        Function<TOutput, bool> condition,
        WaitAllReducer<TOutput, TNext> reducer )
    {
        ArgumentNullException.ThrowIfNull( reducer );

        return async ( context, argument ) =>
        {
            var nextArgument = await pipeline( context, argument ).ConfigureAwait( false );

            if ( condition != null && !condition( context, nextArgument ) )
                return (TNext) (object) nextArgument;

            // mind cancellation and execute
            var contextControl = (IPipelineContextControl) context;

            if ( contextControl.HandleCancellationRequested( nextArgument ) )
                return default;

            using ( contextControl.CreateFrame( context, configure, nameof( WaitAllAsync ) ) )
            {
                return await Next( WaitAllAsync, middleware, context, nextArgument ).ConfigureAwait( false );
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

    private static async Task<TNext> Next<TNext>( FunctionAsync<TOutput, TNext> waitAll, MiddlewareAsync<object, object> middleware, IPipelineContext context, TOutput nextArgument )
    {
        if ( middleware == null )
            return await waitAll( context, nextArgument ).ConfigureAwait( false );

        return (TNext) await middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await waitAll( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}
