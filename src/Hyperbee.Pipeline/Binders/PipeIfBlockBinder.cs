using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class PipeIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public PipeIfBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
        };
    }
}
