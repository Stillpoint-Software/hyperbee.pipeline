using System.Xml.Linq;
using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CallStatementBuilder
{
    public static IPipelineBuilder<TStart, TOutput> Call<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Procedure<TOutput> next, string name
    )
    {
        return CallStatementBuilder<TStart, TOutput>.Call( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TStart, TOutput> Call<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Procedure<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        return CallStatementBuilder<TStart, TOutput>.Call( parent, next, config );
    }

    public static IPipelineBuilder<TStart, TOutput> CallAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ProcedureAsync<TOutput> next,
        string name
    )
    {
        return CallStatementBuilder<TStart, TOutput>.CallAsync( parent, next, config => config.Name = name );
    }

    public static IPipelineBuilder<TStart, TOutput> CallAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ProcedureAsync<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        return CallStatementBuilder<TStart, TOutput>.CallAsync( parent, next, config );
    }
}

internal static class CallStatementBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TOutput> Call(
        IPipelineBuilder<TStart, TOutput> parent,
        Procedure<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new CallStatementBinder<TStart, TOutput>( parentFunction, parentMiddleware, config ).Bind( AsyncNext, next.Method ),
            Middleware = parentMiddleware
        };

        // task wrapper

        Task AsyncNext( IPipelineContext context, TOutput argument )
        {
            next( context, argument );
            return Task.CompletedTask;
        }
    }

    public static IPipelineBuilder<TStart, TOutput> CallAsync(
        IPipelineBuilder<TStart, TOutput> parent,
        ProcedureAsync<TOutput> next,
        Action<IPipelineContext> config = null
    )
    {
        ArgumentNullException.ThrowIfNull( next );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new CallStatementBinder<TStart, TOutput>( parentFunction, parentMiddleware, config ).Bind( next ),
            Middleware = parentMiddleware
        };
    }
}
