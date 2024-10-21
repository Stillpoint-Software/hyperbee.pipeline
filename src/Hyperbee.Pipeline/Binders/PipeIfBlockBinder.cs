using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class PipeIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public PipeIfBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    // public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
    //     };
    // }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var returnLabel = Label( "return" );

        // inner function
        var ctx = Parameter( typeof( IPipelineContext ), "ctx" );
        var arg = Parameter( typeof( TInput ), "arg" );
        var nextExpression = Lambda<FunctionAsync<TOutput, TInput>>(
            BlockAsync(
                Await( Invoke( next, ctx, arg ), configureAwait: false ),
                argument
            ),
            parameters: [ctx, arg]
        );

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceled,
                    Return( returnLabel, Default( typeof(TNext) ) )
                ),
                Label( returnLabel,
                    Await( Invoke( ProcessBlockAsync( nextExpression, context, nextArgument ) ), configureAwait: false )
                )
            ),
            parameters: [context, argument]
        );
    }
}
