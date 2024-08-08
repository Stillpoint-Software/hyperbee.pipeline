using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders.Abstractions;
internal abstract class Binder<TInput, TOutput>
{
    protected FunctionAsync<TInput, TOutput> Pipeline { get; }
    protected Action<IPipelineContext> Configure { get; }

    protected Binder( FunctionAsync<TInput, TOutput> function, Action<IPipelineContext> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    protected virtual async Task<(TOutput Result, bool Canceled)> ProcessPipelineAsync( IPipelineContext context, TInput argument )
    {
        var result = await Pipeline( context, argument ).ConfigureAwait( false );

        var contextControl = (IPipelineContextControl) context;
        var canceled = contextControl.HandleCancellationRequested( result );

        return (canceled ? default : result, canceled);
    }
}
