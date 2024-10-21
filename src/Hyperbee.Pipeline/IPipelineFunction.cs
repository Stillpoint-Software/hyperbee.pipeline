using System.Linq.Expressions;

namespace Hyperbee.Pipeline;

public interface IPipelineFunctionProvider<TInput, TOutput>
{
    // Provide access to the Function and the Middleware
    // so people can implement their own custom binders.
    IPipelineFunction<TInput, TOutput> GetPipelineFunction();
}

public interface IPipelineFunction<TInput, TOutput>
{
    Expression<FunctionAsync<TInput, TOutput>> Function { get; }
    Expression<MiddlewareAsync<object, object>> Middleware { get; }

    void Deconstruct( out Expression<FunctionAsync<TInput, TOutput>> function, out Expression<MiddlewareAsync<object, object>> middleware )
    {
        function = Function;
        middleware = Middleware;
    }
}
