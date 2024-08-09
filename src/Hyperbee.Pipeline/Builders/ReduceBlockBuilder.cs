using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TNext> Reduce<TElement, TNext>( Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder );
    IPipelineBuilder<TInput, TNext> ReduceAsync<TElement, TNext>( bool inheritMiddleware, Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TNext> Reduce<TElement, TNext>( Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
    {
        return ReduceAsync( true, reducer, builder );
    }

    public IPipelineBuilder<TInput, TNext> ReduceAsync<TElement, TNext>( bool inheritMiddleware, Func<TNext, TNext, TNext> reducer, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder<TElement, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( reducer );

        var block = PipelineFactory.Start<TElement>( inheritMiddleware ? Middleware : null );
        var function = ((PipelineBuilder<TInput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new ReduceBlockBinder<TInput, TOutput, TElement, TNext>( reducer, Function ).Bind( function ),
            Middleware = Middleware
        };
    }
}
