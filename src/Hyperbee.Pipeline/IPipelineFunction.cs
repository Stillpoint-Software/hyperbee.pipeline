namespace Hyperbee.Pipeline;

public interface IPipelineFunctionProvider<TInput, TOutput>
{
    // Provide access to the Function and the Middleware
    // so people can implement their own custom binders.
    IPipelineFunction<TInput, TOutput> GetPipelineFunction();
}

public interface IPipelineFunction<TInput, TOutput>
{
    FunctionAsync<TInput, TOutput> Function { get; }
    MiddlewareAsync<object, object> Middleware { get; }

    void Deconstruct( out FunctionAsync<TInput, TOutput> function, out MiddlewareAsync<object, object> middleware )
    {
        function = Function;
        middleware = Middleware;
    }
}
