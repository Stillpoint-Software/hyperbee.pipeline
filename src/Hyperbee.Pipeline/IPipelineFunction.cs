namespace Hyperbee.Pipeline;

public interface IPipelineFunctionProvider<TStart, TOutput>
{
    // Provide access to the Function and the Middleware
    // so people can implement their own custom binders.
    IPipelineFunction<TStart, TOutput> GetPipelineFunction();
}

public interface IPipelineFunction<TStart, TOutput>
{
    FunctionAsync<TStart, TOutput> Function { get; }
    MiddlewareAsync<object, object> Middleware { get; }

    void Deconstruct( out FunctionAsync<TStart, TOutput> function, out MiddlewareAsync<object, object> middleware )
    {
        function = Function;
        middleware = Middleware;
    }
}
