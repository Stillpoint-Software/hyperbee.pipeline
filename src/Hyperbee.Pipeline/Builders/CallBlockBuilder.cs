using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallBlockBuilder
{
    public static IPipelineBuilder<TStart, TOutput> Call<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        return CallBlockBuilder<TStart, TOutput>.Call( parent, true, builder );
    }

    public static IPipelineBuilder<TStart, TOutput> Call<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        return CallBlockBuilder<TStart, TOutput>.Call( parent, inheritMiddleware, builder );
    }
}

internal static class CallBlockBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TOutput> Call(
        IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = builder( block ).CastFunction<TOutput, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new CallBlockBinder<TStart, TOutput>( parentFunction ).Bind( function ),
            Middleware = parentMiddleware
        };
    }
}
