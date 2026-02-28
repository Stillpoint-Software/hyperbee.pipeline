using Hyperbee.Pipeline.Commands;

namespace Hyperbee.Pipeline.Tests.TestSupport;

public class TestPublicCommand : ICommandFunction<string>
{
    public FunctionAsync<Arg.Empty, string> PipelineFunction => throw new NotImplementedException();

    public Task<CommandResult<string>> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<string> { Result = "ok" } );
    }
}

public class TestPublicFunctionCommand : ICommandFunction<int, string>
{
    public FunctionAsync<int, string> PipelineFunction => throw new NotImplementedException();

    public Task<CommandResult<string>> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<string> { Result = "ok" } );
    }

    public Task<CommandResult<string>> ExecuteAsync( int argument, CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<string> { Result = argument.ToString() } );
    }
}

public class TestPublicProcedureCommand : ICommandProcedure<string>
{
    public ProcedureAsync<string> PipelineFunction => throw new NotImplementedException();

    public Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult() );
    }

    public Task<CommandResult> ExecuteAsync( string argument, CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult() );
    }
}

internal class TestInternalCommand : ICommandFunction<int>
{
    public FunctionAsync<Arg.Empty, int> PipelineFunction => throw new NotImplementedException();

    public Task<CommandResult<int>> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<int> { Result = 42 } );
    }
}

public abstract class TestAbstractCommand : ICommandFunction<long>
{
    public abstract FunctionAsync<Arg.Empty, long> PipelineFunction { get; }
    public abstract Task<CommandResult<long>> ExecuteAsync( CancellationToken cancellation = default );
}
