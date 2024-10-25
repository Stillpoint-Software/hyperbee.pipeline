using System.Collections;
using System.Linq.Expressions;
using System.Xml.Linq;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

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

        var result = Variable( typeof( TOutput ), "blockResult" );
        var element = Variable( typeof( TElement ), "element" );
        var enumerator = Variable( typeof( IEnumerator ), "enumerator" );

        var getEnumeratorMethod = Call( nextArguments, typeof( IEnumerable ).GetMethod( "GetEnumerator" )! );
        var moveNextCall = Call( enumerator, typeof( IEnumerator ).GetMethod( "MoveNext" )! );
        var getCurrentMethod = Call( enumerator, typeof( IEnumerator ).GetProperty( "Current" )!.GetMethod! );

        var breakLabel = Label( "breakLoop" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult, nextArguments, enumerator, element, result],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ), configureAwait: false ) ),
                Condition( canceled,
                    Default( typeof( TOutput ) ),
                    Block(
                        Assign( result, nextArgument ),
                        Assign( nextArguments, Convert( nextArgument, typeof( IEnumerable<TElement> ) ) ),
                        Assign( enumerator, getEnumeratorMethod ),
                        Loop(
                            IfThenElse( moveNextCall,
                                Block(
                                    Assign( element, Convert( getCurrentMethod, typeof( TElement ) ) ),
                                    Await( ProcessBlockAsync( next, context, element ), configureAwait: false ),
                                    Empty()
                                ),
                                Break( breakLabel )
                            ),
                            breakLabel
                        ),
                        result
                    )
                )
            ),
            parameters: [context, argument]
        );
    }
}

