namespace Hyperbee.Pipeline.Commands;

public interface ICommand;

public interface ICommandProcedure<in TStart> : ICommand
{
    ProcedureAsync<TStart> PipelineFunction { get; }
    Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult> ExecuteAsync( TStart argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<in TStart, TOutput> : ICommand
{
    FunctionAsync<TStart, TOutput> PipelineFunction { get; }
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
    Task<CommandResult<TOutput>> ExecuteAsync( TStart argument, CancellationToken cancellation = default );
}

public interface ICommandFunction<TOutput> : ICommand
{
    FunctionAsync<Arg.Empty, TOutput> PipelineFunction { get; }
    Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default );
}
