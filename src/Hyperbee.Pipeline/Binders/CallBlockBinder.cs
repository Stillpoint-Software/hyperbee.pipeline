using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public CallBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    // public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TOutput, object> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
    //         return nextArgument;
    //     };
    // }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TOutput, object>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),

                Invoke( LoggerExpression.Log( "CallBlockBinder.Bind" + Random.Shared.Next( 0, 1000 ) ), Convert( nextArgument, typeof( object ) ) ),

                Condition( canceled,
                    Default( typeof( TOutput ) ),
                    // TODO: Think there is a bug here, we shouldn't need a child state machine.

                    Block(
                        Await(
                            ProcessBlockAsync( next, context, nextArgument ),
                            configureAwait: false
                        ),
                        nextArgument
                    )

                )
            ),
            parameters: [context, argument]
        );
    }
}
