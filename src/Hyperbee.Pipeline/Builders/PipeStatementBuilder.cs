using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class PipeStatementBuilder
{
    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Function<TOutput, TNext> next,
        string name
    )
    {
        return PipeStatementBuilder<TInput, TOutput>.Pipe( parent, next, ctx => ctx.Name = name );
    }

    public static IPipelineBuilder<TInput, TNext> Pipe<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Function<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        return PipeStatementBuilder<TInput, TOutput>.Pipe( parent, next, config );
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        string name
    )
    {
        return PipeStatementBuilder<TInput, TOutput>.PipeAsync( parent, next, ctx => ctx.Name = name );
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        return PipeStatementBuilder<TInput, TOutput>.PipeAsync( parent, next, config );
    }
}

internal static class PipeStatementBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TNext> Pipe<TNext>(
        IPipelineBuilder<TInput, TOutput> parent,
        Function<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        Expression<FunctionAsync<TOutput, TNext>> nextExpression = ( ctx, arg ) => AsyncNext( next, ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, configExpression ).Bind( nextExpression ),
            Middleware = parentMiddleware
        };
    }

    public static IPipelineBuilder<TInput, TNext> PipeAsync<TNext>(
        IPipelineBuilder<TInput, TOutput> parent,
        FunctionAsync<TOutput, TNext> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        Expression<FunctionAsync<TOutput, TNext>> nextExpression = ( ctx, arg ) => next( ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new PipeStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, configExpression ).Bind( nextExpression ),
            Middleware = parentMiddleware
        };
    }

    private static async Task<TNext> AsyncNext<TNext>( Function<TOutput, TNext> next, IPipelineContext ctx, TOutput arg )
    {
        var result = next( ctx, arg );
        await Task.CompletedTask.ConfigureAwait( false );

        return result;
    }

}
