using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TOutput> Call( Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder );
    IPipelineBuilder<TInput, TOutput> Call( bool inheritMiddleware, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    // Call an inner builder discarding the final result. Acts like an Action.

    public IPipelineBuilder<TInput, TOutput> Call(
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        return Call( true, builder );
    }

    public IPipelineBuilder<TInput, TOutput> Call( bool inheritMiddleware,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder> builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var block = PipelineFactory.Start<TOutput>( inheritMiddleware ? Middleware : null );
        var function = builder( block ).CastFunction<TOutput, object>(); // cast because we don't know the final Pipe output value

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallBlockBinder<TInput, TOutput>( Function ).Bind( ExpressionBinder.ToExpression( function ) ),
            Middleware = Middleware
        };
    }
}
