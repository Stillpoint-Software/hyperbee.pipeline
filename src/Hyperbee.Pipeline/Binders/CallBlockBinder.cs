using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TStart, TOutput> : BlockBinder<TStart, TOutput>
{
    public CallBlockBinder( FunctionAsync<TStart, TOutput> function )
        : base( function, default )
    {
    }

    public FunctionAsync<TStart, TOutput> Bind( FunctionAsync<TOutput, object> next )
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
