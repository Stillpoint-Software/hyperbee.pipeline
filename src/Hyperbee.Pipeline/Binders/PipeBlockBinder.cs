using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    // public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
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
        var argument = Parameter( typeof( TNext ), "argument" );

        var awaitedResult = Variable( typeof( (TNext, bool) ), "awaitedResult" );
        var nextArgumentField = Field( awaitedResult, "Item1" );
        var canceledField = Field( awaitedResult, "Item2" );

        var returnLabel = Label( "return" );

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceledField,
                    Return( returnLabel, Default( typeof( TNext ) ) )
                ),
                Label( returnLabel,
                    Await( Invoke( ProcessBlockAsync( next, context, nextArgumentField ) ), configureAwait: false )
                )
            ),
            parameters: [context, argument]
        );
    }
}
