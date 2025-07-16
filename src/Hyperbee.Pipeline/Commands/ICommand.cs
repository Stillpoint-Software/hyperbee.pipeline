namespace Hyperbee.Pipeline.Commands;

public interface ICommand;

public interface ICommandProcedure<in TStart> : ICommand
{
    Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult> ExecuteAsync( TStart argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<in TStart, TOutput> : ICommand
{
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult<TOutput>> ExecuteAsync( TStart argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<TOutput> : ICommand
{
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
}
