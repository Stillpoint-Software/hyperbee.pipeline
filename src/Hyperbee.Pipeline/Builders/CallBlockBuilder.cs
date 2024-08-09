using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallBlockBuilder
{
    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return CallBlockBuilder<TInput, TOutput>.Call( parent, true, builder );
    }

    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> parent, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return CallBlockBuilder<TInput, TOutput>.Call( parent, inheritMiddleware, builder );
    }
}


static class CallBlockBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TOutput> Call( IPipelineBuilder<TInput, TOutput> parent, bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var (parentFunction,parentMiddleware) = parent.GetPipelineFunction();

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null );
        var function = builder( block ).CastFunction<TOutput, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput> { Function = new CallBlockBinder<TInput, TOutput>( parentFunction ).Bind( function ), Middleware = parentMiddleware };
    }
}
