using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class Binder<TStart, TOutput>
{
    protected FunctionAsync<TStart, TOutput> Pipeline { get; }
    protected Action<IPipelineContext> Configure { get; }

    protected Binder( FunctionAsync<TStart, TOutput> function, Action<IPipelineContext> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    protected virtual async Task<(TOutput Result, bool Canceled)> ProcessPipelineAsync( IPipelineContext context, TStart argument )
    {
        var result = await Pipeline( context, argument ).ConfigureAwait( false );

        var contextControl = (IPipelineContextControl) context;
        var canceled = contextControl.HandleCancellationRequested( result );

        return (canceled ? default : result, canceled);
    }
}
