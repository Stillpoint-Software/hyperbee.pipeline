namespace Hyperbee.Pipeline.Extensions.Implementation;

public static class BuilderImplExtensions
{
    public static IPipelineFunction<TStart, TOutput> GetPipelineFunction<TStart, TOutput>( this IPipelineBuilder<TStart, TOutput> builder )
    {
        var provider = builder as IPipelineFunctionProvider<TStart, TOutput>;
        return provider?.GetPipelineFunction();
    }
}
