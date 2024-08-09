using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallIfBuilder
{
    public static IPipelineBuilder<TInput, TOutput> CallIf<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return CallIfBuilder<TInput, TOutput>.CallIf( parent, condition, true, builder );
    }
    public static IPipelineBuilder<TInput, TOutput> CallIf<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return CallIfBuilder<TInput, TOutput>.CallIf( parent, condition, inheritMiddleware, builder );
    }
}

public static class CallIfBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TOutput> CallIf( IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, bool> condition, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = builder( block ).CastFunction<TOutput, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput> { Function = new CallIfBlockBinder<TInput, TOutput>( condition, parentFunction ).Bind( function ), Middleware = parentMiddleware };
    }
}
