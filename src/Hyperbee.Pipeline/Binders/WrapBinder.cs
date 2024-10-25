using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
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

    // public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TInput, TOutput> next )
    // {
    //     var defaultName = next.Method.Name;
    //
    //     return async ( context, argument ) =>
    //     {
    //         var contextControl = (IPipelineContextControl) context;
    //
    //         using var _ = contextControl.CreateFrame( context, Configure, defaultName );
    //
    //         return await Middleware(
    //             context,
    //             argument,
    //             async ( context1, argument1 ) => await next( context1, argument1 ).ConfigureAwait( false )
    //         ).ConfigureAwait( false );
    //     };
    // }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TInput, TOutput>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        // if ( Middleware == null )
        //     return await nextFunction( context, nextArgument ).ConfigureAwait( false );
        if ( Middleware == null )
        {
            return Lambda<FunctionAsync<TInput, TOutput>>(
                Invoke( next, context, argument ),
                parameters: [context, argument] );
        }

        // async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
        var context1 = Parameter( typeof( IPipelineContext ), "context1" );
        var argument1 = Parameter( typeof( TOutput ), "argument1" );

        var middlewareNext = Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                Convert( Await(
                        Invoke( next, context1, argument1 ),
                        configureAwait: false ),
                    typeof( TOutput ) )
            ),
            parameters: [context1, argument1]
        );

        // return (TNext) await Middleware(
        //     context,
        //     nextArgument,
        //     middlewareNext
        // ).ConfigureAwait( false );
        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
            Convert(
                Await(
                    Invoke( Middleware,
                        context,
                        argument,
                        middlewareNext
                    ),
                    configureAwait: false ),
                typeof( TOutput ) ) ),
            parameters: [context, argument] );

    }
}
