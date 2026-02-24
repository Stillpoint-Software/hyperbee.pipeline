namespace Hyperbee.Pipeline;

// Generic Type Parameters and Monadic Composition
// ================================================
//
// The pipeline is monadic. With TStart fixed, PipelineBuilder<TStart, _> forms
// a monad over the second type parameter. More precisely, each pipeline step is
// a Kleisli arrow (A -> Task<B>), and Pipe is Kleisli composition. This is
// analogous to how Reader<R, _> or State<S, _> monads fix their first parameter
// and operate monadically on the second.
//
// Monad operations:
//   return/pure : PipelineFactory.Start<TStart>()  -- wraps identity
//   bind (>>=)  : Binder.Bind<TNext>(...)          -- composes next step
//
// Builder methods are forward-looking: they always build the 'next' step.
//
// The PipelineBuilder<>'s Function property is the current pipeline function.
// The function is a composition function representing all the previous steps
// where:
//      TStart  is the input type to the first step (invariant through composition)
//      TOutput is the output type of the last step
//      TNext   is the output type of the next step (the builder is building)
//
// Conceptually: CurrentBuilder().NextBuilder<TOutput,TNext>(..)
//
// Every delegate in the pipeline (FunctionAsync, Function, etc.) uses TStart as
// its first type parameter. This is intentional: each function is itself a
// composable pipeline (a Kleisli arrow), so TStart means "the start of this
// composition" at every level. When a step function is typed as
// FunctionAsync<TOutput, TNext>, TOutput is that step's own TStart.
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
