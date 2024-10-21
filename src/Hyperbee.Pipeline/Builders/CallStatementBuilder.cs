using System.Linq.Expressions;
using System.Xml.Linq;
using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;
using static System.Net.Mime.MediaTypeNames;

namespace Hyperbee.Pipeline;

public static class CallStatementBuilder
{
    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Procedure<TOutput> next, string name
    )
    {
        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => Task.Run( () => next( ctx, arg ) );
        Expression<Action<IPipelineContext>> configExpression = name == null
            ? null
            : ctx => SetName( ctx, name );

        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, nextExpression, configExpression );
    }

    public static IPipelineBuilder<TInput, TOutput> Call<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Procedure<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => Task.Run( () => next( ctx, arg ) );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, nextExpression, configExpression );
    }

    public static IPipelineBuilder<TInput, TOutput> CallAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        ProcedureAsync<TOutput> next,
        string name
    )
    {
        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => next( ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = name == null
            ? null
            : ctx => SetName( ctx, name );

        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, nextExpression, configExpression );
    }

    public static IPipelineBuilder<TInput, TOutput> CallAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        ProcedureAsync<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        Expression<ProcedureAsync<TOutput>> nextExpression = ( ctx, arg ) => next( ctx, arg );
        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        return CallStatementBuilder<TInput, TOutput>.CallAsync( parent, nextExpression, configExpression );
    }
    public static void SetName( IPipelineContext ctx, string name )
    {
        ctx.Name = name;
    }
}

internal static class CallStatementBuilder<TInput, TOutput>
{
    // public static IPipelineBuilder<TInput, TOutput> Call(
    //     IPipelineBuilder<TInput, TOutput> parent,
    //     Procedure<TOutput> next,
    //     Action<IPipelineContext> config = null
    // )
    // {
    //     ArgumentNullException.ThrowIfNull( next );
    //
    //     var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();
    //
    //     return new PipelineBuilder<TInput, TOutput>
    //     {
    //         Function = new CallStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, config ).Bind( AsyncNext, next.Method ),
    //         Middleware = parentMiddleware
    //     };
    //
    //     // task wrapper
    //
    //     Task AsyncNext( IPipelineContext context, TOutput argument )
    //     {
    //         next( context, argument );
    //         return Task.CompletedTask;
    //     }
    // }

    public static IPipelineBuilder<TInput, TOutput> CallAsync(
        IPipelineBuilder<TInput, TOutput> parent,
        Expression<ProcedureAsync<TOutput>> next,
        Expression<Action<IPipelineContext>> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new CallStatementBinder<TInput, TOutput>( parentFunction, parentMiddleware, config ).Bind( next ),
            Middleware = parentMiddleware
        };
    }
}
