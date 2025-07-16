using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TStart, TOutput> : BlockBinder<TStart, TOutput>
{
    public PipeBlockBinder( FunctionAsync<TStart, TOutput> function )
        : base( function, default )
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
