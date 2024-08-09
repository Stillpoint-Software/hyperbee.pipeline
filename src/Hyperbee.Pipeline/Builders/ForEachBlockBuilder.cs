using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class ForEachBlockBuilder
{
    public static ForEachBlockBuilderWrapper<TInput, TOutput> ForEach<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent )
    {
        return new ForEachBlockBuilderWrapper<TInput, TOutput>( parent );
    }

    public class ForEachBlockBuilderWrapper<TInput, TOutput>( IPipelineBuilder<TInput, TOutput> parent )
    {
        public IPipelineBuilder<TInput, TOutput> Type<TElement>( Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder )
        {
            return ForEachBlockBuilder<TInput, TOutput, TElement>.ForEach( parent, true, builder );
        }

        public IPipelineBuilder<TInput, TOutput> Type<TElement>( bool inheritMiddleware, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder )
        {
            return ForEachBlockBuilder<TInput, TOutput, TElement>.ForEach( parent, inheritMiddleware, builder );
        }
    }
}

public static class ForEachBlockBuilder<TInput, TOutput, TElement>
{
    public static IPipelineBuilder<TInput, TOutput> ForEach(
        IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder
    )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TElement>( inheritMiddleware ? parentMiddleware : null );
        var function = builder( block ).CastFunction<TElement, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new ForEachBlockBinder<TInput, TOutput, TElement>( parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}

