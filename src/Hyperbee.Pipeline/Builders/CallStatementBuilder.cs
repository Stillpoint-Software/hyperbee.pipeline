using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TOutput> Call( Procedure<TOutput> next, string name );
    IPipelineBuilder<TInput, TOutput> Call( Procedure<TOutput> next, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TOutput> CallAsync( ProcedureAsync<TOutput> next, string name );
    IPipelineBuilder<TInput, TOutput> CallAsync( ProcedureAsync<TOutput> next, Action<IPipelineContext> config = null );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TOutput> Call( Procedure<TOutput> next, string name ) => Call( next, config => config.Name = name );

    public IPipelineBuilder<TInput, TOutput> Call( Procedure<TOutput> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallStatementBinder<TInput, TOutput>( Function, Middleware, config ).Bind( AsyncNext, next.Method ),
            Middleware = Middleware
        };

        // task wrapper
        
        Task AsyncNext( IPipelineContext context, TOutput argument )
        {
            next( context, argument );
            return Task.CompletedTask;
        }
    }

    public IPipelineBuilder<TInput, TOutput> CallAsync( ProcedureAsync<TOutput> next, string name ) => CallAsync( next, config => config.Name = name );

    public IPipelineBuilder<TInput, TOutput> CallAsync( ProcedureAsync<TOutput> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallStatementBinder<TInput, TOutput>( Function, Middleware, config ).Bind( next ),
            Middleware = Middleware
        };
    }
}