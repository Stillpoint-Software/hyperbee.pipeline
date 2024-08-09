using Hyperbee.Pipeline.Binders.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WaitAllBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    private MiddlewareAsync<object, object> Middleware { get; }

    public WaitAllBlockBinder( 
        Expression<FunctionAsync<TInput, TOutput>> function, 
        MiddlewareAsync<object, object> middleware, 
        Action<IPipelineContext> configure )
        : base( null, function, configure )
    {
        Middleware = middleware;
    }

    public WaitAllBlockBinder( 
        Function<TOutput, bool> condition,
        Expression<FunctionAsync<TInput, TOutput>> function,
        MiddlewareAsync<object, object> middleware,
        Action<IPipelineContext> configure )
        : base( condition, function, configure )
    {
        Middleware = middleware;
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( FunctionAsync<TOutput, object>[] nexts, WaitAllReducer<TOutput, TNext> reducer )
    {
        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( WaitAllBlockBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!
            .MakeGenericMethod( typeof( TNext ) );

        // Create parameters for the lambda expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        // Create a call expression to the helper method
        var callBindImplAsync = Expression.Call(
            Expression.Constant( this ),
            bindImplAsyncMethodInfo,
            Expression.Constant( nexts ),
            Expression.Constant( reducer ),
            Pipeline,
            paramContext,
            paramArgument
        );

        // Create and return the final expression
        return Expression.Lambda<FunctionAsync<TInput, TNext>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TNext> BindImplAsync<TNext>(
        FunctionAsync<TOutput, object>[] nexts,
        WaitAllReducer<TOutput, TNext> reducer,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument )
    {
        var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        // WaitAllBlockBinder is unique in that it is both a block configure and a step.
        // The reducer is the step action, and because it is a step, we need to ensure
        // that middleware is called. Middleware requires us to pass in the execution
        // function that it wraps. This requires an additional level of wrapping.

        return await WaitAllAsync( context, nextArgument, nexts, reducer ).ConfigureAwait( false );
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
