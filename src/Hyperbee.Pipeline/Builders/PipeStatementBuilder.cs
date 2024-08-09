using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeStatementBuilder
{
    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, TNext> next, string name )
    {
        return PipeStatementBuilder<TInput, TOutput>.Pipe( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>( this IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        return PipeStatementBuilder<TInput, TOutput>.Pipe( parent, next, config );
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TInput, TOutput, TNext>( this IPipelineBuilder<TInput, TOutput> parent, FunctionAsync<TOutput, TNext> next, string name )
    {
        return PipeStatementBuilder<TInput, TOutput>.PipeAsync( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TInput, TOutput, TNext>( this IPipelineBuilder<TInput, TOutput> parent, FunctionAsync<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        return PipeStatementBuilder<TInput, TOutput>.PipeAsync( parent, next, config );
    }

}

public static class PipeStatementBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TNext> Pipe<TNext>( IPipelineBuilder<TInput, TOutput> parent, Function<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, config ).Bind( AsyncNext, next.Method ),
            Middleware = parentMiddleware
        };

        // task wrapper

        Task<TNext> AsyncNext( IPipelineContext context, TOutput argument )
        {
            return Task.FromResult( next( context, argument ) );
        }
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TNext>( IPipelineBuilder<TInput, TOutput> parent, FunctionAsync<TOutput, TNext> next, Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, config ).Bind( next ),
            Middleware = parentMiddleware
        };
    }
}
