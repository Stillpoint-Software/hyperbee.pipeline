using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TNext> Pipe<TNext>( Function<TOutput, TNext> next, string name );
    IPipelineBuilder<TInput, TNext> Pipe<TNext>( Function<TOutput, TNext> next, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TNext> PipeAsync<TNext>( FunctionAsync<TOutput, TNext> next, string name );
    IPipelineBuilder<TInput, TNext> PipeAsync<TNext>( FunctionAsync<TOutput, TNext> next, Action<IPipelineContext> config = null );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TNext> Pipe<TNext>( Function<TOutput, TNext> next, string name ) => Pipe( next, config => config.Name = name );

    public IPipelineBuilder<TInput, TNext> Pipe<TNext>( Function<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>(
                Function,
                Middleware,
                config ).Bind( AsyncNext, next.Method ),
            Middleware = Middleware
        };

        // task wrapper

        Task<TNext> AsyncNext( IPipelineContext context, TOutput argument )
        {
            return Task.FromResult( next( context, argument ) );
        }
    }

    public IPipelineBuilder<TInput, TNext> PipeAsync<TNext>( FunctionAsync<TOutput, TNext> next, string name ) => PipeAsync( next, config => config.Name = name );

    public IPipelineBuilder<TInput, TNext> PipeAsync<TNext>( FunctionAsync<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>(
                Function,
                Middleware,
                config ).Bind( next ),
            Middleware = Middleware
        };
    }
}
