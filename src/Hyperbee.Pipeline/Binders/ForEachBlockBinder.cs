using System.Linq.Expressions;
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

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TElement, object>> next )
    {
        /*
        {
            return async ( context, argument ) =>
            {
                var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

                if ( canceled )
                    return default;

                var nextArguments = (IEnumerable<TElement>) nextArgument;

                foreach ( var elementArgument in nextArguments )
                {
                    await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
                }

                return nextArgument;
            };
        }
        */

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var nextArguments = Variable( typeof( IEnumerable<TElement> ), "nextArguments" );
        var element = Variable( typeof( TElement ), "element" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult, nextArguments],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ), configureAwait: false ) ),
                Condition( canceled,
                    Default( typeof( TOutput ) ),
                    Block(
                        Assign( nextArguments, Convert( nextArgument, typeof( IEnumerable<TElement> ) ) ),
                        ForEach( nextArgument, element,
                            Await( ProcessBlockAsync( next, context, element ), configureAwait: false )
                        ),
                        nextArgument
                    )
                )
            ),
            parameters: [context, argument]
        );
    }
}

