using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class PipeIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public PipeIfBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        /*
        {
            return async ( context, argument ) =>
            {
                var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    
                if ( canceled )
                    return default;
    
                return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
            };
        }
        */

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),
                Condition( canceled,
                    Default( typeof( TNext ) ),
                    ProcessBlockAsync( next, context, nextArgument )
                )
            ),
            parameters: [context, argument]
        );
    }
}
