using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    private Expression<MiddlewareAsync<object, object>> Middleware { get; }
    private Expression<Action<IPipelineContext>> Configure { get; }

    public CallStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
    {
        Pipeline = function;
        Middleware = middleware;
        Configure = configure;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<ProcedureAsync<TOutput>> next, MethodInfo method = null )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( CallStatementBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            next,
            Pipeline,
            Configure,
            Expression.Constant( method, typeof( MethodInfo ) )
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>(callBind, paramContext, paramArgument);
    }

    public FunctionAsync<TInput, TOutput> BindImpl( 
        ProcedureAsync<TOutput> next, 
        FunctionAsync<TInput, TOutput> pipeline,
        MiddlewareAsync<object, object> middleware,
        Action<IPipelineContext> configure,
        MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        return async ( context, argument ) =>
        {
            var nextArgument = await pipeline( context, argument ).ConfigureAwait( false );

            var contextControl = (IPipelineContextControl) context;

            if ( contextControl.HandleCancellationRequested( nextArgument ) )
                return default;

            using ( contextControl.CreateFrame( context, configure, defaultName ) )
            {
                return await Next( next, middleware, context, nextArgument ).ConfigureAwait( false );
            }
        };
    }

    private async Task<TOutput> Next( ProcedureAsync<TOutput> next, MiddlewareAsync<object, object> middleware, IPipelineContext context, TOutput nextArgument )
    {
        if ( Middleware == null )
        {
            await next( context, nextArgument ).ConfigureAwait( false );
            return nextArgument;
        }

        await middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) =>
            {
                await next( context1, (TOutput) argument1 ).ConfigureAwait( false );
                return nextArgument;
            }
        ).ConfigureAwait( false );

        return nextArgument;
    }
}
