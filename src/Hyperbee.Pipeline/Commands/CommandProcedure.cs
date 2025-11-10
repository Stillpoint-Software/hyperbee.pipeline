using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Commands;

public abstract class CommandProcedure<TStart> : ICommandProcedure<TStart>
{
    private IPipelineContextFactory ContextFactory { get; }
    protected Lazy<ProcedureAsync<TStart>> Pipeline { get; }
    protected ILogger Logger { get; }

    protected CommandProcedure( IPipelineContextFactory pipelineContextFactory, ILogger logger )
    {
        ContextFactory = pipelineContextFactory;
        Pipeline = new Lazy<ProcedureAsync<TStart>>( CreatePipeline );
        Logger = logger;
    }

    protected abstract ProcedureAsync<TStart> CreatePipeline();

    public virtual Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default ) => ExecuteAsync( default, cancellation );

    public virtual async Task<CommandResult> ExecuteAsync( TStart argument, CancellationToken cancellation = default )
    {
        var context = ContextFactory.Create( Logger, cancellation );

        await Pipeline.Value( context, argument ).ConfigureAwait( false );

        return new CommandResult
        {
            Context = context,
            CommandType = GetType()
        };
    }
}
