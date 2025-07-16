namespace Hyperbee.Pipeline;

// A quick note about generic arguments. Remember that the builder methods are
// forward-looking and are always building the 'next' step.
//
// The PipelineBuilder<>'s Function property is the current pipeline function.
// The function is a composition function representing all the previous steps
// where:
//      TStart  is the input type to the first step
//      TOutput is the output type of the last step
//      TNext   is the output type of the next step (the builder is building)
//
// Conceptually: CurrentBuilder().NextBuilder<TOutput,TNext>(..)
//
public class PipelineFactory
{
    public static IPipelineStartBuilder<TStart, TStart> Start<TStart>()
    {
        return new PipelineBuilder<TStart, TStart>
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

    internal static IPipelineStartBuilder<TStart, TStart> Start<TStart>( MiddlewareAsync<object, object> functionMiddleware )
    {
        return new PipelineBuilder<TStart, TStart>
        {
            Function = ( context, argument ) => Task.FromResult( argument ),
            Middleware = functionMiddleware
        };
    }
}
