using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallStatementBuilder
{
    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Procedure<TOutput> next,
        string name
    )
    {
        return CallStatementBuilder<TInput, TOutput>.Call( parent, next, ctx => ctx.Name = name );
    }

    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Procedure<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        return CallStatementBuilder<TInput, TOutput>.Call( parent, next, config );
    }

    public static IPipelineBuilder<TInput, TOutput> CallAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        ProcedureAsync<TOutput> next,
        string name
    )
    {
        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, next, ctx => ctx.Name = name );
    }

    public static IPipelineBuilder<TInput, TOutput> CallAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        ProcedureAsync<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, next, config );
    }
}

internal static class CallStatementBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TOutput> Call(
        IPipelineBuilder<TInput, TOutput> parent,
        Procedure<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => AsyncNext( next, ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, configExpression ).Bind( nextExpression, next.Method ),
            Middleware = parentMiddleware
        };
    }

    public static IPipelineBuilder<TInput, TOutput> CallAsync(
        IPipelineBuilder<TInput, TOutput> parent,
        ProcedureAsync<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => next( ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, configExpression ).Bind( nextExpression ),
            Middleware = parentMiddleware
        };
    }
    internal static Task AsyncNext( Procedure<TOutput> next, IPipelineContext context, TOutput argument )
    {
        next( context, argument );
        return Task.CompletedTask;
    }
}
