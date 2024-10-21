using System.Collections;
using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext> : BlockBinder<TInput, TOutput>
{
    private Expression<Func<TNext, TNext, TNext>> Reducer { get; }

    public ReduceBlockBinder( Expression<Func<TNext, TNext, TNext>> reducer, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
        Reducer = reducer;
    }

    // public FunctionAsync<TInput, TNext> Bind( FunctionAsync<TElement, TNext> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         var nextArguments = (IEnumerable<TElement>) nextArgument;
    //         var accumulator = default( TNext );
    //
    //         // Process each element and apply the reducer
    //         foreach ( var elementArgument in nextArguments )
    //         {
    //             var result = await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
    //             accumulator = Reducer( accumulator, result );
    //         }
    //
    //         return accumulator;
    //     };
    // }

    public Expression<FunctionAsync<TInput, TNext>> Bind( Expression<FunctionAsync<TElement, TNext>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TNext, bool) ), "awaitedResult" );
        var blockResult = Variable( typeof( (TNext, bool) ), "blockResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var nextArguments = Variable( typeof( IEnumerable<TElement> ), "nextArguments" );
        var moveNextCall = Call( nextArguments, typeof( IEnumerator ).GetMethod( "MoveNext" )! );

        var breakLabel = Label( "breakLoop" );
        var returnLabel = Label( "return" );

        // TODO: get next result and call accumulator/reducer

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceled,
                    Return( returnLabel, Default( typeof( TNext ) ) )
                ),
                Assign( nextArguments, Convert( nextArgument, typeof( IEnumerable<TElement> ) ) ),

                Loop(
                    IfThenElse( IsTrue( moveNextCall ),
                        Await( Invoke( ProcessBlockAsync( next, context, nextArgument ) ), configureAwait: false ),
                        Label( breakLabel )
                    ),
                    breakLabel
                ),

                Label( returnLabel, nextArgument )
            ),
            parameters: [context, argument]
        );
    }
}

