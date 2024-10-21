using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class CallIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public CallIfBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
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
        var nextArgumentField = Field( awaitedResult, "Item1" );
        var canceledField = Field( awaitedResult, "Item2" );

        var returnLabel = Label( "return" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceledField,
                    Return( returnLabel, Default( typeof( TOutput ) ) )
                ),
                Await( Invoke( ProcessBlockAsync( next, context, nextArgumentField ) ), configureAwait: false ),
                Label( returnLabel, nextArgumentField )
            ),
            parameters: [context, argument]
        );
    }
}
