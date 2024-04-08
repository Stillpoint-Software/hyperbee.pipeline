using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Commands;

public abstract class CommandFunction<TInput, TOutput> : ICommandFunction<TInput, TOutput>
{
    private IPipelineContextFactory ContextFactory { get; }
    protected Lazy<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    protected ILogger Logger { get; }

    protected CommandFunction( IPipelineContextFactory pipelineContextFactory, ILogger logger )
    {
        ContextFactory = pipelineContextFactory;
        Pipeline = new Lazy<FunctionAsync<TInput, TOutput>>( CreatePipeline );
        Logger = logger;
    }

    protected abstract FunctionAsync<TInput, TOutput> CreatePipeline();

    public virtual Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default ) => ExecuteAsync( default, cancellation );

    public virtual async Task<CommandResult<TOutput>> ExecuteAsync( TInput argument, CancellationToken cancellation = default )
    {
        var context = ContextFactory.Create( Logger );

        return new CommandResult<TOutput>
        {
            Result = await Pipeline.Value( context, argument ).ConfigureAwait( false ),
            Context = context,
            CommandType = GetType()
        };
    }
}

public abstract class CommandFunction<TOutput> : ICommandFunction<TOutput>
{
    private IPipelineContextFactory ContextFactory { get; }
    protected Lazy<FunctionAsync<Arg.Empty, TOutput>> Pipeline { get; }
    protected ILogger Logger { get; }

    protected CommandFunction( IPipelineContextFactory pipelineContextFactory, ILogger logger )
    {
        ContextFactory = pipelineContextFactory;
        Pipeline = new Lazy<FunctionAsync<Arg.Empty, TOutput>>( CreatePipeline );
        Logger = logger;
    }

    protected abstract FunctionAsync<Arg.Empty, TOutput> CreatePipeline();

    public virtual async Task<CommandResult<TOutput>> ExecuteAsync( CancellationToken cancellation = default )
    {
        var context = ContextFactory.Create( Logger );

        return new CommandResult<TOutput>
        {
            Result = await Pipeline.Value( context ).ConfigureAwait( false ),
            Context = context,
            CommandType = GetType()
        };
    }
}