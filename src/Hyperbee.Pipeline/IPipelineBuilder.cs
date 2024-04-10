using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public delegate Task<TOutput> MiddlewareAsync<TInput, TOutput>( IPipelineContext context, TInput argument, FunctionAsync<TInput, TOutput> next );

public delegate Task<TOutput> FunctionAsync<in TInput, TOutput>( IPipelineContext context, TInput argument = default );

public delegate TOutput Function<in TInput, out TOutput>( IPipelineContext context, TInput argument = default );

public delegate Task ProcedureAsync<in TInput>( IPipelineContext context, TInput argument = default );

public delegate void Procedure<in TInput>( IPipelineContext context, TInput argument = default );

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

public partial interface IPipelineStartBuilder<TInput, TOutput> : IPipelineBuilder<TInput, TOutput>
{
    // head actions: (e.g. Hook) that are only valid at the start of the pipeline 
}

public partial interface IPipelineBuilder<TInput, TOutput> : IPipelineFinalBuilder<TInput, TOutput>
{
    // normal actions
}

public interface IPipelineFinalBuilder<in TInput, TOutput> : IPipelineBuilder
{
    // tail actions
    FunctionAsync<TInput, TOutput> Build();
    ProcedureAsync<TInput> BuildAsProcedure();
}

public interface IPipelineBuilder
{
    // no actions allowed that modify the pipeline

    // used to bind the tail of an inner builder to the continuation of its parent.
    // this is necessary because you can't directly cast delegates. 
    FunctionAsync<TIn, TOut> CastFunction<TIn, TOut>();
}
