using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TOutput> CallIf( Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder );
    IPipelineBuilder<TInput, TOutput> CallIf( Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TOutput> CallIf( Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return CallIf( condition, true, builder );
    }

    public IPipelineBuilder<TInput, TOutput> CallIf( Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? Middleware : null );
        var function = ((PipelineBuilder<TOutput, object>) builder( block )).Function;  // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallIfBlockBinder<TInput, TOutput>( condition, Function ).Bind( function ),
            Middleware = Middleware
        };
    }
}
