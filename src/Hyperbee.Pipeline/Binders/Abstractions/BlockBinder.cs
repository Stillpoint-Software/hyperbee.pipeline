using Hyperbee.Pipeline.Context;
using System.Threading.Tasks;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class BlockBinder<TStart, TOutput> : Binder<TStart, TOutput>
{
    protected BlockBinder( FunctionAsync<TStart, TOutput> function, Action<IPipelineContext> configure )
        : base( function, configure )
    {
    }

    // Using TArgument instead of TOutput allows more capabilities for special
    // use cases where the next argument is not the same as the output type
    // like ReduceBlockBinder and ForEachBlockBinder

    protected virtual ValueTask<TNext> ProcessBlockAsync<TArgument, TNext>( FunctionAsync<TArgument, TNext> blockFunction, IPipelineContext context, TArgument nextArgument )
    {
        // If the function completes synchronously, avoid async state machine
        var task = blockFunction( context, nextArgument );
        
        if (task.IsCompletedSuccessfully)
            return new ValueTask<TNext>(task.Result);

        return new ValueTask<TNext>(task);
    }
}
