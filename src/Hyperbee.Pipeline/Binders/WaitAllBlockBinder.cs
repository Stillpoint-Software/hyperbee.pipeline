using Hyperbee.Pipeline.Binders.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;


internal class WaitAllBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    private Expression<MiddlewareAsync<object, object>> Middleware { get; }

    public WaitAllBlockBinder( 
        Expression<FunctionAsync<TInput, TOutput>> function, 
        Expression<MiddlewareAsync<object, object>> middleware, 
        Expression<Action<IPipelineContext>> configure )
        : base( null, function, configure )
    {
        Middleware = middleware;
    }

    public WaitAllBlockBinder( 
        Expression<Function<TOutput, bool>> condition,
        Expression<FunctionAsync<TInput, TOutput>> function,
        Expression<MiddlewareAsync<object, object>> middleware,
        Expression<Action<IPipelineContext>> configure )
        : base( condition, function, configure )
    {
        Middleware = middleware;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind<TNext>( FunctionAsync<TInput, TOutput>[] next, WaitAllReducer<TOutput, TNext> reducer )
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

    public FunctionAsync<TInput, TNext> BindImpl<TNext>( 
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
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

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
