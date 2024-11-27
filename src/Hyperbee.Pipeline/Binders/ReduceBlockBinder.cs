using System.Collections;
using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext> : BlockBinder<TInput, TOutput>
{
    private Expression<Func<TNext, TNext, TNext>> Reducer { get; }

    public ReduceBlockBinder( Expression<Func<TNext, TNext, TNext>> reducer, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
        Reducer = reducer;
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind( Expression<FunctionAsync<TElement, TNext>> next )
    {
        /*
        {
            return async( context, argument ) =>
            {
                var( nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

                if (canceled )
                    return default;

                var nextArguments = (IEnumerable<TElement>) nextArgument;
                var accumulator = default( TNext );

                // Process each element and apply the reducer
                foreach (var elementArgument in nextArguments )
                {
                    var result = await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
                    accumulator = Reducer( accumulator, result );
                }

                return accumulator;
            };
        }
        */

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var blockResult = Variable( typeof( TNext ), "blockResult" );
        var accumulator = Variable( typeof( TNext ), "accumulator" );
        var finalResult = Variable( typeof( TNext ), "finalResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var nextArguments = Variable( typeof( IEnumerable<TElement> ), "nextArguments" );
        var element = Variable( typeof( TElement ), "element" );

        // TODO: IfThenElse should be switched Condition, and we should be able to remove finalResult (bug in expressions)
        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult, nextArguments, /*enumerator,*/ element, accumulator, finalResult, blockResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ), configureAwait: false ) ),
                IfThenElse( canceled,
                    Default( typeof( TNext ) ),
                    Block(
                        Assign( nextArguments, Convert( nextArgument, typeof( IEnumerable<TElement> ) ) ),
                        Assign( accumulator, Default( typeof( TNext ) ) ),
                        ForEach( nextArguments, element,
                            Block(
                                Assign( blockResult, Await( ProcessBlockAsync( next, context, element ), configureAwait: false ) ),
                                Assign( accumulator, Invoke( Reducer, accumulator, blockResult ) )
                            )
                        ),
                        Assign( finalResult, accumulator )
                    )
                ),
                finalResult
            ),
            parameters: [context, argument]
        );
    }
}

