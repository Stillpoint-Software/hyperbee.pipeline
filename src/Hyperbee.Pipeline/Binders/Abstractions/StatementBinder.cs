using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
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

    // protected MiddlewareAsync<object, object> Middleware1 { get; }
    // protected virtual async Task<TNext> ProcessStatementAsync<TNext>( FunctionAsync<TOutput, TNext> nextFunction, IPipelineContext context, TOutput nextArgument, string frameName )
    // {
    //     var contextControl = (IPipelineContextControl) context;
    //
    //     using var _ = contextControl.CreateFrame( context, Configure, frameName );
    //
    //     if ( Middleware1 == null )
    //         return await nextFunction( context, nextArgument ).ConfigureAwait( false );
    //
    //     return (TNext) await Middleware1(
    //         context,
    //         nextArgument,
    //         async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
    //     ).ConfigureAwait( false );
    // }

    protected virtual Expression ProcessStatementAsync<TNext>( Expression<FunctionAsync<TOutput, TNext>> nextFunction,
        ParameterExpression context, Expression nextArgument, string frameName )
    {
        // if ( Middleware == null )
        //     return await nextFunction( context, nextArgument ).ConfigureAwait( false );
        if ( Middleware == null )
        {
            return Invoke( nextFunction, context, nextArgument );

            //using var _ = contextControl.CreateFrame( context, Configure, frameName );
            // return CreateFrameExpression(
            //          Convert( context, typeof(IPipelineContextControl) ),
            //          context,
            //          Configure,
            //          Await( Invoke( nextFunction, context, nextArgument ), configureAwait: false ),
            //          frameName );
        }

        // async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
        var context1 = Parameter( typeof( IPipelineContext ), "context1" );
        var argument1 = Parameter( typeof( object ), "argument1" );

        var middlewareNext = Lambda<FunctionAsync<object, object>>(
            BlockAsync(
                Convert( Await(
                        Invoke( nextFunction, context1, Convert( argument1, typeof( TOutput ) ) ),
                        configureAwait: false ),
                    typeof( object ) )
            ),
            parameters: [context1, argument1]
        );

        // return (TNext) await Middleware(
        //     context,
        //     nextArgument,
        //     middlewareNext
        // ).ConfigureAwait( false );
        //return
        //using var _ = contextControl.CreateFrame( context, Configure, frameName );
        // CreateFrameExpression(
        //     Convert( Constant( context ), typeof(IPipelineContextControl) ),
        //     context,
        //     Configure,

        var returnResult = Variable( typeof( TNext ), "returnResult" );

        var b = BlockAsync(
                [returnResult],
                //Invoke( LoggerExpression.Log( "StatementBinder.ProcessStatementAsync" + Random.Shared.Next( 0, 1000 ) ), Convert( nextArgument, typeof( object ) ) ),

                Assign( returnResult, Convert(
                    Await(
                        Invoke( Middleware,
                            context,
                            Convert( nextArgument, typeof(object)),
                            middlewareNext
                        ),
                        configureAwait: false ),
                    typeof( TNext ) ) )

                //Invoke( LoggerExpression.Log( "StatementBinder.ProcessStatementAsync-" + Random.Shared.Next( 0, 1000 ) ), Convert( returnResult, typeof( object ) ) )

                , returnResult
                ); //,


        // frameName ); //);

        return b;
    }

    public static Expression CreateFrameExpression(
        Expression controlParam,
        Expression contextParam,
        Expression<Action<IPipelineContext>> config,
        Expression body,
        string defaultName = null
    )
    {
        var nameVariable = Variable( typeof( string ), "originalName" );
        var idVariable = Variable( typeof( int ), "originalId" );

        var idProperty = Property( controlParam, "Id" );
        var nameProperty = Property( controlParam, "Name" );

        return BlockAsync(
            [nameVariable, idVariable],
            Assign( idVariable, idProperty ),
            Assign( nameVariable, nameProperty ),
            TryFinally(
                Block(
                    Assign( idProperty, Call( controlParam, "GetNextId", Type.EmptyTypes ) ),
                    Assign( nameProperty, Constant( defaultName ) ),
                    config != null
                        ? Invoke( config, contextParam )
                        : Empty(),
                    body
                ),
                Block(
                    Assign( idProperty, idVariable ),
                    Assign( nameProperty, nameVariable )
                ) )
        );
    }
}
