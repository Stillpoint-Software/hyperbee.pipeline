using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TNext> Pipe<TNext>( Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder );
    IPipelineBuilder<TInput, TNext> Pipe<TNext>( bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    // Pipe the result of an inner builder to the next pipeline step. Acts like a Func.

    public IPipelineBuilder<TInput, TNext> Pipe<TNext>( Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        return Pipe( true, builder );
    }

    public IPipelineBuilder<TInput, TNext> Pipe<TNext>( bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? Middleware : null );
        var function = ((PipelineBuilder<TOutput, TNext>) builder( block )).Function;

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeBlockBinder<TInput, TOutput>( Function ).Bind( function ),
            Middleware = Middleware
        };
    }
}
