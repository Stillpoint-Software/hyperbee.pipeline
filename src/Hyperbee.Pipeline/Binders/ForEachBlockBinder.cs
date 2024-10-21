using System.Collections;
using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement> : BlockBinder<TInput, TOutput>
{
    public ForEachBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    // public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TElement, object> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         var nextArguments = (IEnumerable<TElement>) nextArgument;
    //
    //         foreach ( var elementArgument in nextArguments )
    //         {
    //             await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
    //         }
    //
    //         return nextArgument;
    //     };
    // }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TElement, object>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var nextArguments = Variable( typeof( IEnumerable<TElement> ), "nextArguments" );
        var moveNextCall = Call( nextArguments, typeof( IEnumerator ).GetMethod( "MoveNext" )! );

        var breakLabel = Label( "breakLoop" );
        var returnLabel = Label( "return" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceled,
                    Return( returnLabel, Default( typeof( TOutput ) ) )
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

