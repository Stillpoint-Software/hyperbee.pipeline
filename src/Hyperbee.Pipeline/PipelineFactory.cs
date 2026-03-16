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
    /// <summary>
    /// Starts a pipeline with an identity function. This is the monadic 'return' or 'pure' operation, which lifts a value into the pipeline context.
    /// </summary>
    /// <typeparam name="TStart"></typeparam>
    /// <returns></returns>
    public static IPipelineStartBuilder<TStart, TStart> Start<TStart>()
    {
        return new PipelineBuilder<TStart, TStart>
        {
            Function = ( context, argument ) => Task.FromResult( argument )
        };
    }

    /// <summary>
    /// Starts a pipeline with an identity function and an initial middleware. This overload allows you to provide middleware that will be applied at the start of the pipeline, before any steps are configured. The provided middleware will wrap the initial identity function, allowing you to inject behavior right from the beginning of the pipeline execution.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a pipeline by applying a configuration function.
    /// </summary>
    /// <typeparam name="TStart">The input type of the pipeline.</typeparam>
    /// <typeparam name="TOutput">The output type of the pipeline.</typeparam>
    /// <param name="configure">A function that configures the pipeline steps.</param>
    /// <returns>The built pipeline function.</returns>
    public static FunctionAsync<TStart, TOutput> Create<TStart, TOutput>(
        Func<IPipelineStartBuilder<TStart, TStart>, IPipelineBuilder<TStart, TOutput>> configure
    )
    {
        ArgumentNullException.ThrowIfNull( configure );

        return configure( Start<TStart>() )
            .Build();
    }

    /// <summary>
    /// Creates a pipeline with middleware from a provider applied automatically.
    /// Hooks are applied after Start, wraps are applied before Build.
    /// </summary>
    /// <typeparam name="TStart">The input type of the pipeline.</typeparam>
    /// <typeparam name="TOutput">The output type of the pipeline.</typeparam>
    /// <param name="provider">The middleware provider supplying hooks and wraps.</param>
    /// <param name="configure">A function that configures the pipeline steps.</param>
    /// <returns>The built pipeline function.</returns>
    public static FunctionAsync<TStart, TOutput> Create<TStart, TOutput>(
        IPipelineMiddlewareProvider provider,
        Func<IPipelineStartBuilder<TStart, TStart>, IPipelineBuilder<TStart, TOutput>> configure
    )
    {
        ArgumentNullException.ThrowIfNull( configure );

        var builder = Start<TStart>()
            .UseHooks( provider );

        return configure( builder )
            .UseWraps( provider )
            .Build();
    }
}
