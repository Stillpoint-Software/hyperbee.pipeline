using System.Linq.Expressions;

namespace Hyperbee.Pipeline;

// A quick note about generic arguments. Remember that the builder methods are
// forward-looking and are always building the 'next' step.
//
// The PipelineBuilder<>'s Function property is the current pipeline function.
// The function is a composition function representing all the previous steps
// where:
//      TInput  is the input type to the first step
//      TOutput is the output type of the last step
//      TNext   is the output type of the next step (the builder is building)
//
// Conceptually: CurrentBuilder().NextBuilder<TOutput,TNext>(..)
//
public class PipelineFactory
{
    public static IPipelineStartBuilder<TInput, TInput> Start<TInput>()
    {
        return new PipelineBuilder<TInput, TInput>
        {
            Function = ( context, argument ) => Task.FromResult( argument )
        };
    }

    public static IPipelineStartBuilder<Arg.Empty, Arg.Empty> Start()
    {
        return new PipelineBuilder<Arg.Empty, Arg.Empty>
        {
            Function = ( context, argument ) => Task.FromResult( argument )
        };
    }

    internal static IPipelineStartBuilder<TInput, TInput> Start<TInput>( MiddlewareAsync<object, object> functionMiddleware )
    {
        // IPipelineContext context, TInput argument, FunctionAsync<TInput, TOutput> next
        return new PipelineBuilder<TInput, TInput>
        {
            Function = ( context, argument ) => Task.FromResult( argument ),
            Middleware = functionMiddleware
        };
    }
}
