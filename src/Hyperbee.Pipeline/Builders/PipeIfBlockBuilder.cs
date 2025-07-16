using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeIfBlockBuilder
{
    public static IPipelineBuilder<TStart, TNext> PipeIf<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        return PipeIfBlockBuilder<TStart, TOutput>.PipeIf( parent, condition, true, builder );
    }

    public static IPipelineBuilder<TStart, TNext> PipeIf<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        return PipeIfBlockBuilder<TStart, TOutput>.PipeIf( parent, condition, inheritMiddleware, builder );
    }
}

internal static class PipeIfBlockBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TNext> PipeIf<TNext>(
        IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder
    )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = ((PipelineBuilder<TOutput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new PipeIfBlockBinder<TStart, TOutput>( condition, parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}
