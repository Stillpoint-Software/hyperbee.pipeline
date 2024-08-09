using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TNext> PipeIf<TNext>( Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder );
    IPipelineBuilder<TInput, TNext> PipeIf<TNext>( Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TNext> PipeIf<TNext>( Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        return PipeIf( condition, true, builder );
    }

    public IPipelineBuilder<TInput, TNext> PipeIf<TNext>( Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? Middleware : null );
        var function = ((PipelineBuilder<TOutput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeIfBlockBinder<TInput, TOutput>(
                condition,
                Function ).Bind( function ),
            Middleware = Middleware
        };
    }
}
