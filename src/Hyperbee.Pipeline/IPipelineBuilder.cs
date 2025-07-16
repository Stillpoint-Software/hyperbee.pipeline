using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public delegate Task<TOutput> MiddlewareAsync<TStart, TOutput>( IPipelineContext context, TStart argument, FunctionAsync<TStart, TOutput> next );

public delegate Task<TOutput> FunctionAsync<in TStart, TOutput>( IPipelineContext context, TStart argument = default );

public delegate TOutput Function<in TStart, out TOutput>( IPipelineContext context, TStart argument = default );

public delegate Task ProcedureAsync<in TStart>( IPipelineContext context, TStart argument = default );

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
