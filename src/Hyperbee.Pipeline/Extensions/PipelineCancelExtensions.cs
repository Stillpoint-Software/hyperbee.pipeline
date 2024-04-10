using Hyperbee.Pipeline.Context;

// ReSharper disable CheckNamespace
namespace Hyperbee.Pipeline;

public static class PipelineCancelExtensions
{
    public static IPipelineBuilder<TInput, TNext> CancelWith<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> pipeline,
        Func<IPipelineContext, TOutput, TNext> result )
    {
        // usage: .CancelWith( (context, argument) => result )
        return pipeline
            .Pipe( ( context, argument ) =>
            {
                context.CancelAfter();
                return result( context, argument );
            } );
    }

    public static IPipelineBuilder<TInput, TOutput> Cancel<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> pipeline )
    {
        // usage: .Cancel()
        return pipeline.Call( ( context, _ ) => context.CancelAfter() );
    }

    public static IPipelineBuilder<TInput, TOutput> CancelIf<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> pipeline,
        Func<IPipelineContext, TOutput, bool> condition )
    {
        // usage: .CancelIf( (context, argument) => argument == test )
        return pipeline
            .Pipe( ( context, argument ) =>
            {
                if ( condition( context, argument ) )
                    context.CancelAfter();

                return argument;
            } );
    }
}
