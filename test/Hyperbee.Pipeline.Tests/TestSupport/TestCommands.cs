using Hyperbee.Pipeline.Commands;

namespace Hyperbee.Pipeline.Tests.TestSupport;

public class TestPublicCommand : ICommandFunction<string>
{
    public Task<CommandResult<string>> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<string> { Result = "ok" } );
    }
}

public class TestPublicFunctionCommand : ICommandFunction<int, string>
{
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
    public Task<CommandResult<int>> ExecuteAsync( CancellationToken cancellation = default )
    {
        return Task.FromResult( new CommandResult<int> { Result = 42 } );
    }
}

public abstract class TestAbstractCommand : ICommandFunction<long>
{
    public abstract Task<CommandResult<long>> ExecuteAsync( CancellationToken cancellation = default );
}
