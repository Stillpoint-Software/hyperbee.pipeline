using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class PipeIfBlockBinder<TStart, TOutput> : ConditionalBlockBinder<TStart, TOutput>
{
    public PipeIfBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TStart, TOutput> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TStart, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
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
