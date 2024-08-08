using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class CallIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public CallIfBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TInput, TOutput> Bind( Expression<FunctionAsync<TOutput, object>> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
            return nextArgument;
        };
    }
}
