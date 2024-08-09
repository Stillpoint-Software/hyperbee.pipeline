using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeBlockBuilderExtensions
{
    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        return PipeBlockBuilder<TInput, TOutput, TNext>.Pipe( parent, true, builder );
    }

    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        return PipeBlockBuilder<TInput, TOutput, TNext>.Pipe( parent, inheritMiddleware, builder );
    }
}

internal static class PipeBlockBuilder<TInput, TOutput, TNext>
{
    public static IPipelineBuilder<TInput, TNext> Pipe(
        IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = ((PipelineBuilder<TOutput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeBlockBinder<TInput, TOutput>( parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}

