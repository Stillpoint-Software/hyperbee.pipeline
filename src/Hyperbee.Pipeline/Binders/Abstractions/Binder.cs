using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class Binder<TInput, TOutput>
{
    protected Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    protected Action<IPipelineContext> Configure { get; }

    protected Binder( Expression<FunctionAsync<TInput, TOutput>> function, Action<IPipelineContext> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    protected virtual async Task<(TOutput Result, bool Canceled)> ProcessPipelineAsync( IPipelineContext context, TInput argument, FunctionAsync<TInput, TOutput> pipeline )
    {
        var result = await pipeline( context, argument ).ConfigureAwait( false );

        var contextControl = (IPipelineContextControl) context;
        var canceled = contextControl.HandleCancellationRequested( result );

        return (canceled ? default : result, canceled);
    }
}
