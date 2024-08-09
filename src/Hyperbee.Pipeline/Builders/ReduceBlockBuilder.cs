using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class ReduceBlockBuilder
{
    public static ReduceBlockBuilderWrapper<TInput, TOutput> Reduce<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent )
    {
        return new ReduceBlockBuilderWrapper<TInput, TOutput>( parent );
    }

    public class ReduceBlockBuilderWrapper<TInput, TOutput>( IPipelineBuilder<TInput, TOutput> parent )
    {
        public IPipelineBuilder<TInput, TNext> Type<TElement, TNext>( Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
        {
            return ReduceBlockBuilder<TInput, TOutput, TElement, TNext>.ReduceAsync( parent, true, reducer, builder );
        }

        public IPipelineBuilder<TInput, TNext> Type<TElement, TNext>( bool inheritMiddleware, Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
        {
            return ReduceBlockBuilder<TInput, TOutput, TElement, TNext>.ReduceAsync( parent, inheritMiddleware, reducer, builder );
        }
    }
}

public static class ReduceBlockBuilder<TInput, TOutput, TElement, TNext>
{
    public static IPipelineBuilder<TInput, TNext> ReduceAsync(
        IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<TNext, TNext, TNext> reducer,
        Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TElement>( inheritMiddleware ? parentMiddleware : null );
        var function = ((PipelineBuilder<TElement, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new ReduceBlockBinder<TInput, TOutput, TElement, TNext>( reducer, parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}
