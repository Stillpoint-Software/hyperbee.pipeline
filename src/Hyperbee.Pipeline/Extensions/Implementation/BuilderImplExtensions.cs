namespace Hyperbee.Pipeline.Extensions.Implementation;

public static class BuilderImplExtensions
{
    public static IPipelineFunction<TInput, TOutput> GetPipelineFunction<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> builder )
    {
        var provider = builder as IPipelineFunctionProvider<TInput, TOutput>;
        return provider?.GetPipelineFunction();
    }
}
