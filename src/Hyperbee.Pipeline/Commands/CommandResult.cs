using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Commands;

public class CommandResult
{
    public IPipelineContext Context;
    public Type CommandType { get; init; }

    public void Deconstruct( out IPipelineContext context )
    {
        context = Context;
    }
}

public class CommandResult<TOutput> : CommandResult
{
    public TOutput Result;

    public void Deconstruct( out IPipelineContext context, out TOutput result )
    {
        context = Context;
        result = Result;
    }
}
