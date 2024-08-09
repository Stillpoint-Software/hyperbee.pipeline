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
    MiddlewareAsync<object, object> Middleware { get; }
}
