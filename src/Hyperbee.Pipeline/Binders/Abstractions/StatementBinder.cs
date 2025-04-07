using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class StatementBinder<TInput, TOutput> : Binder<TInput, TOutput>
{
    protected Expression<MiddlewareAsync<object, object>> Middleware { get; }

    protected StatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( function, configure )
    {
        Middleware = middleware;
    }

    protected virtual Expression ProcessStatementAsync<TNext>( Expression<FunctionAsync<TOutput, TNext>> nextFunction,
        ParameterExpression context, Expression nextArgument, string frameName )
    {
        /*
        {
            var contextControl = (IPipelineContextControl) context;

            using var _ = contextControl.CreateFrame( context, Configure, frameName );

            if ( Middleware1 == null )
                return await nextFunction( context, nextArgument ).ConfigureAwait( false );

            return (TNext) await Middleware1(
                context,
                nextArgument,
                async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
            ).ConfigureAwait( false );
        }
        */

        if ( Middleware == null )
        {
            var disposableVar1 = Parameter( typeof( IDisposable ), Guid.NewGuid().ToString( "N" ) );
            return BlockAsync(
                Using( //using var _ = contextControl.CreateFrame( context, Configure, frameName );
                    disposableVar1,
                    ContextImplExtensions.CreateFrameExpression( context, Configure, frameName ),
                    Await( Invoke( nextFunction, context, nextArgument ) )
                ) );
        }

        // async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
        var context1 = Parameter( typeof( IPipelineContext ), "context1" );
        var argument1 = Parameter( typeof( object ), "argument1" );

        var middlewareNext = Lambda<FunctionAsync<object, object>>(
            BlockAsync(
                Convert(
                    Await( Invoke( nextFunction, context1, Convert( argument1, typeof( TOutput ) ) ), configureAwait: false ),
                    typeof( object ) )
            ),
            parameters: [context1, argument1]
        );
        var disposableVar2 = Parameter( typeof( IDisposable ), Guid.NewGuid().ToString( "N" ) );
        return BlockAsync(
                    Using( //using var _ = contextControl.CreateFrame( context, Configure, frameName );
                        disposableVar2,
                        ContextImplExtensions.CreateFrameExpression( context, Configure, frameName ),
                        Convert(
                            Await(
                                Invoke( Middleware,
                                    context,
                                    Convert( nextArgument, typeof( object ) ),
                                    middlewareNext
                                ),
                                configureAwait: false ),
                            typeof( TNext ) )
                        )
                     );
    }
}
