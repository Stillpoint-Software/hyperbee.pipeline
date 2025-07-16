using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallIfBuilder
{
    public static IPipelineBuilder<TStart, TOutput> CallIf<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        return CallIfBuilder<TStart, TOutput>.CallIf( parent, condition, true, builder );
    }
    public static IPipelineBuilder<TStart, TOutput> CallIf<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        return CallIfBuilder<TStart, TOutput>.CallIf( parent, condition, inheritMiddleware, builder );
    }
}

internal static class CallIfBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TOutput> CallIf(
        IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = builder( block ).CastFunction<TOutput, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new CallIfBlockBinder<TStart, TOutput>( condition, parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}
