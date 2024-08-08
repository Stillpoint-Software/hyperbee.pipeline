using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class CallIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public CallIfBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TOutput, object> next )
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
