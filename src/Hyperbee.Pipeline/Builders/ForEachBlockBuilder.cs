using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TOutput> ForEach<TElement>( Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder );
    IPipelineBuilder<TInput, TOutput> ForEachAsync<TElement>( bool inheritMiddleware, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TOutput> ForEach<TElement>( Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder )
    {
        return ForEachAsync( true, builder );
    }

    public IPipelineBuilder<TInput, TOutput> ForEachAsync<TElement>( bool inheritMiddleware, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var block = PipelineFactory.Start<TElement>( inheritMiddleware ? Middleware : null );
        var function = builder( block ).CastFunction<TElement, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new ForEachBlockBinder<TInput, TOutput, TElement>( Function ).Bind( function ),
            Middleware = Middleware
        };
    }
}
