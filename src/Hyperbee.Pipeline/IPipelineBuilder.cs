using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

// Pipeline delegate definitions.
//
// These delegates are the fundamental building blocks for pipeline composition.
// They use TStart/TOutput as type parameter names because every function in
// the pipeline is itself a composable unit — a Kleisli arrow — where TStart
// represents "the start of this composition" at every level of abstraction.
//
// At the pipeline level, TStart is the invariant input type set by
// PipelineFactory.Start<TStart>(). At the step level, when a delegate is
// instantiated as FunctionAsync<TOutput, TNext>, TOutput becomes the
// TStart of that step's own composition. This self-similar naming reflects
// the monadic nature of the pipeline: every step is itself a pipeline.

/// <summary>
/// An asynchronous middleware function that wraps a pipeline step or group of steps.
/// The middleware receives the current context and argument, and a <paramref name="next"/>
/// delegate representing the wrapped pipeline segment.
/// </summary>
/// <typeparam name="TStart">The input type to this composition. At the pipeline level this
/// is the invariant start type; at the step level it is the input to the wrapped segment.</typeparam>
/// <typeparam name="TOutput">The output type produced by this composition.</typeparam>
public delegate Task<TOutput> MiddlewareAsync<TStart, TOutput>( IPipelineContext context, TStart argument, FunctionAsync<TStart, TOutput> next );

/// <summary>
/// An asynchronous pipeline function that transforms an input into an output.
/// This is the core delegate for pipeline composition — each step, each binder,
/// and each composed pipeline is represented as a <see cref="FunctionAsync{TStart, TOutput}"/>.
/// </summary>
/// <typeparam name="TStart">The input type to this composition. When used as the full pipeline
/// function, this is the invariant start type. When used as a step function (e.g.,
/// <c>FunctionAsync&lt;TOutput, TNext&gt;</c>), TOutput from the previous step becomes this
/// step's TStart — reflecting the Kleisli arrow composition.</typeparam>
/// <typeparam name="TOutput">The output type produced by this function.</typeparam>
public delegate Task<TOutput> FunctionAsync<in TStart, TOutput>( IPipelineContext context, TStart argument = default );

/// <summary>
/// A synchronous pipeline function that transforms an input into an output.
/// Synchronous functions are internally wrapped as <see cref="FunctionAsync{TStart, TOutput}"/>
/// during pipeline composition.
/// </summary>
/// <typeparam name="TStart">The input type to this composition.</typeparam>
/// <typeparam name="TOutput">The output type produced by this function.</typeparam>
public delegate TOutput Function<in TStart, out TOutput>( IPipelineContext context, TStart argument = default );

/// <summary>
/// An asynchronous pipeline procedure that processes an input without producing a return value.
/// Used by <c>Call</c> operations and <c>BuildAsProcedure</c>.
/// </summary>
/// <typeparam name="TStart">The input type to this composition.</typeparam>
public delegate Task ProcedureAsync<in TStart>( IPipelineContext context, TStart argument = default );

/// <summary>
/// A synchronous pipeline procedure that processes an input without producing a return value.
/// </summary>
/// <typeparam name="TStart">The input type to this composition.</typeparam>
public delegate void Procedure<in TStart>( IPipelineContext context, TStart argument = default );

public struct Arg
{
    // Convenience structure for pipelines that want an empty start.
    // example PipelineBuilder.Start<Arg.Empty>();

    public struct Empty;
}

// pipeline builders have constraints about where they can be applied when creating a pipeline.
//
// start builders:
//   pipeline functions, like hook middleware, that can only be applied at the start of a pipeline.
//
// action builders:
//   pipeline functions, like Pipe and Call, that must come after hooks but before the build operation.
//
// final builders:
//   pipeline functions, like build, that must come last.
// 
// we solve for this using interfaces.

public interface IPipelineStartBuilder<in TStart, TOutput> : IPipelineBuilder<TStart, TOutput>
{
    // head actions: (e.g. Hook) that are only valid at the start of the pipeline 
}

public interface IPipelineBuilder<in TStart, TOutput> : IPipelineFinalBuilder<TStart, TOutput>
{
    // normal actions
}

public interface IPipelineFinalBuilder<in TStart, TOutput> : IPipelineBuilder
{
    // tail actions
    FunctionAsync<TStart, TOutput> Build();
    ProcedureAsync<TStart> BuildAsProcedure();
}

public interface IPipelineBuilder
{
    // no actions allowed that modify the pipeline

    // used to bind the tail of an inner builder to the continuation of its parent.
    // this is necessary because you can't directly cast delegates. 
    FunctionAsync<TIn, TOut> CastFunction<TIn, TOut>();
}
