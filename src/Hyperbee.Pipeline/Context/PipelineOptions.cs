namespace Hyperbee.Pipeline.Context;

public sealed class PipelineOptions
{
    // When true (default), a step that fails with an exception halts pipeline
    // progression: the exception is captured on the context and the pipeline
    // short-circuits to the boundary instead of running subsequent steps on
    // default data. Set false to restore the prior run-through behavior.

    public bool HaltOnError { get; set; } = true;
}
