using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Commands;

public abstract class CommandProcedure<TInput> : ICommandProcedure<TInput>
{
    private IPipelineContextFactory ContextFactory { get; }
    protected Lazy<ProcedureAsync<TInput>> Pipeline { get; }
    protected ILogger Logger { get; }

    protected CommandProcedure( IPipelineContextFactory pipelineContextFactory, ILogger logger )
    {
        ContextFactory = pipelineContextFactory;
        Pipeline = new Lazy<ProcedureAsync<TInput>>( CreatePipeline );
        Logger = logger;
    }

    protected abstract ProcedureAsync<TInput> CreatePipeline();

    public virtual Task<CommandResult> ExecuteAsync( CancellationToken cancellation = default ) => ExecuteAsync( default, cancellation );

    public virtual async Task<CommandResult> ExecuteAsync( TInput argument, CancellationToken cancellation = default )
    {
        var context = ContextFactory.Create( Logger );

        await Pipeline.Value( context, argument ).ConfigureAwait( false );

        return new CommandResult
        {
            Context = context,
            CommandType = GetType()
        };
    }
}
