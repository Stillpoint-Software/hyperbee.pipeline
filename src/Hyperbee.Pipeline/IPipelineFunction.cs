namespace Hyperbee.Pipeline;

public interface IPipelineFunctionProvider<in TInput, TOutput>
{
    // Provide access to the Function and the Middleware
    // so people can implement their own custom binders.
    IPipelineFunction<TInput, TOutput> GetPipelineFunction();
}

public interface IPipelineFunction<in TInput, TOutput>
{
    FunctionAsync<TInput, TOutput> Function { get; }
    MiddlewareAsync<object, object> Middleware { get; }
}