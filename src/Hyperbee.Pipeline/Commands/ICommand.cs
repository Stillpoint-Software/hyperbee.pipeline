namespace Hyperbee.Pipeline.Commands;

public interface ICommand;

public interface ICommandProcedure<in TInput> : ICommand
{
    Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult> ExecuteAsync( TInput argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<in TInput, TOutput> : ICommand
{
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult<TOutput>> ExecuteAsync( TInput argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<TOutput> : ICommand
{
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
}