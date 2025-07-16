using Hyperbee.Pipeline.Context;

// ReSharper disable CheckNamespace
namespace Hyperbee.Pipeline;

public static class PipelineCancelExtensions
{
    public static IPipelineBuilder<TStart, TNext> CancelWith<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> pipeline,
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

    public static IPipelineBuilder<TStart, TOutput> Cancel<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> pipeline )
    {
        // usage: .Cancel()
        return pipeline.Call( ( context, _ ) => context.CancelAfter() );
    }

    public static IPipelineBuilder<TStart, TOutput> CancelIf<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> pipeline,
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
