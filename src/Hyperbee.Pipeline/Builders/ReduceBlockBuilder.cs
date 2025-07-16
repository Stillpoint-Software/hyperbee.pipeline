using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class ReduceBlockBuilder
{
    public static ReduceBlockBuilderWrapper<TStart, TOutput> Reduce<TStart, TOutput>( this IPipelineBuilder<TStart, TOutput> parent )
    {
        return new ReduceBlockBuilderWrapper<TStart, TOutput>( parent );
    }

    public class ReduceBlockBuilderWrapper<TStart, TOutput>( IPipelineBuilder<TStart, TOutput> parent )
    {
        public IPipelineBuilder<TStart, TNext> Type<TElement, TNext>(
            Func<TNext, TNext, TNext> reducer,
            Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder
        )
        {
            return ReduceBlockBuilder<TStart, TOutput, TElement, TNext>.ReduceAsync( parent, true, reducer, builder );
        }

        public IPipelineBuilder<TStart, TNext> Type<TElement, TNext>(
            bool inheritMiddleware,
            Func<TNext, TNext, TNext> reducer,
            Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder
        )
        {
            return ReduceBlockBuilder<TStart, TOutput, TElement, TNext>.ReduceAsync( parent, inheritMiddleware, reducer, builder );
        }
    }
}

internal static class ReduceBlockBuilder<TStart, TOutput, TElement, TNext>
{
    public static IPipelineBuilder<TStart, TNext> ReduceAsync(
        IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<TNext, TNext, TNext> reducer,
        Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TElement>( inheritMiddleware ? parentMiddleware : null );
        var function = ((PipelineBuilder<TElement, TNext>) builder( block )).Function;

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new ReduceBlockBinder<TStart, TOutput, TElement, TNext>( reducer, parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}
