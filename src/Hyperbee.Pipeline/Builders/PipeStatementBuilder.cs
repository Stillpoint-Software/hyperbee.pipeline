using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeStatementBuilder
{
    public static IPipelineBuilder<TStart, TNext> Pipe<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, TNext> next,
        string name
    )
    {
        return PipeStatementBuilder<TStart, TOutput>.Pipe( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TStart, TNext> Pipe<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        return PipeStatementBuilder<TStart, TOutput>.Pipe( parent, next, config );
    }

    public static IPipelineBuilder<TStart, TNext> PipeAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        string name
    )
    {
        return PipeStatementBuilder<TStart, TOutput>.PipeAsync( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TStart, TNext> PipeAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        return PipeStatementBuilder<TStart, TOutput>.PipeAsync( parent, next, config );
    }
}

internal static class PipeStatementBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TNext> Pipe<TNext>(
        IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new PipeStatementBinder<TStart, TOutput>( parentFunction, parentMiddleware, config ).Bind( AsyncNext, next.Method ),
            Middleware = parentMiddleware
        };

        // task wrapper

        Task<TNext> AsyncNext( IPipelineContext context, TOutput argument )
        {
            return Task.FromResult( next( context, argument ) );
        }
    }

    public static IPipelineBuilder<TStart, TNext> PipeAsync<TNext>(
        IPipelineBuilder<TStart, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new PipeStatementBinder<TStart, TOutput>( parentFunction, parentMiddleware, config ).Bind( next ),
            Middleware = parentMiddleware
        };
    }
}
