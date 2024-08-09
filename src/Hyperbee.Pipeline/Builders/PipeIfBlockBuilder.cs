using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeIfBlockBuilder
{
    public static IPipelineBuilder<TInput, TNext> PipeIf<TInput,TOutput,TNext>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        return PipeIfBlockBuilder<TInput, TOutput>.PipeIf( parent, condition, true, builder );
    }

    public static IPipelineBuilder<TInput, TNext> PipeIf<TInput, TOutput, TNext>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        return PipeIfBlockBuilder<TInput, TOutput>.PipeIf( parent, condition, inheritMiddleware, builder );
    }
}

public static class PipeIfBlockBuilder<TInput, TOutput> 
{
    public static IPipelineBuilder<TInput, TNext> PipeIf<TNext>( IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = ((PipelineBuilder<TOutput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext> 
        { 
            Function = new PipeIfBlockBinder<TInput, TOutput>( condition, parentFunction ).Bind( function ), 
            Middleware = parentMiddleware 
        };
    }
}
