using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class WrapBinder<TInput, TOutput>
{
    private Expression<MiddlewareAsync<TInput, TOutput>> Middleware { get; }
    private Expression<Action<IPipelineContext>> Configure { get; }

    public WrapBinder( Expression<MiddlewareAsync<TInput, TOutput>> middleware, Expression<Action<IPipelineContext>> configure )
    {
        Middleware = middleware;
        Configure = configure;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TInput, TOutput>> next )
    {
        /*
        var defaultName = next.Method.Name;
        
        return async ( context, argument ) =>
        {
            var contextControl = (IPipelineContextControl) context;
        
            using var _ = contextControl.CreateFrame( context, Configure, defaultName );
        
            return await Middleware(
                context,
                argument,
                async ( ctx, arg ) => await next( context1, argument1 ).ConfigureAwait( false )
            ).ConfigureAwait( false );
        };
        */

        // TODO: Better way to get Name
        var frameName = next.Name ?? "name";

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        // If there is no middleware, there is no need to wrap the next function
        if ( Middleware == null )
        {
            var disposableVar1 = Parameter( typeof( IDisposable ), Guid.NewGuid().ToString( "N" ) );
            return Lambda<FunctionAsync<TInput, TOutput>>(
                Using( //using var _ = contextControl.CreateFrame( context, Configure, frameName );
                    disposableVar1,
                    ContextImplExtensions.CreateFrameExpression( context, Configure, frameName ),
                    Invoke( next, context, argument )
                ),
                parameters: [context, argument] );
        }

        var ctx = Parameter( typeof( IPipelineContext ), "ctx" );
        var arg = Parameter( typeof( TOutput ), "arg" );
        var middlewareNext = Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                Convert( Await(
                        Invoke( next, ctx, arg ),
                        configureAwait: false ),
                    typeof( TOutput ) )
            ),
            parameters: [ctx, arg]
        );

        var disposableVar2 = Parameter( typeof( IDisposable ), Guid.NewGuid().ToString( "N" ) );
        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                Using( //using var _ = contextControl.CreateFrame( context, Configure, frameName );
                    disposableVar2,
                    ContextImplExtensions.CreateFrameExpression( context, Configure, frameName ),
                    Await(
                        Invoke( Middleware,
                            context,
                            argument,
                            middlewareNext
                        ),
                        configureAwait: false )
                )
            ),
            parameters: [context, argument] );

    }
}
