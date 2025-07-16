using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeBlockBuilder
{
    public static IPipelineBuilder<TStart, TNext> Pipe<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        return Pipe( parent, true, builder );
    }

    public static IPipelineBuilder<TStart, TNext> Pipe<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
            ArgumentNullException.ThrowIfNull(builder);

            var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();
            var block = PipelineFactory.Start<TOutput>(inheritMiddleware ? parentMiddleware : null);
            var function = ((PipelineBuilder<TOutput, TNext>)builder(block)).Function;

            return new PipelineBuilder<TStart, TNext>
            {
                Function = new PipeBlockBinder<TStart, TOutput>(parentFunction).Bind(function),
                Middleware = parentMiddleware
            };
    }
}

