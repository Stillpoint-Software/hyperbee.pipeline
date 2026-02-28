using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class CommandStatementBuilder
{
    // PipeAsync: transform value through a command's pipeline

    public static IPipelineBuilder<TStart, TNext> PipeAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ICommandFunction<TOutput, TNext> command,
        string name
    )
    {
        return parent.PipeAsync( command.PipelineFunction, name );
    }

    public static IPipelineBuilder<TStart, TNext> PipeAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ICommandFunction<TOutput, TNext> command,
        Action<IPipelineContext> config = null
    )
    {
        return parent.PipeAsync( command.PipelineFunction, config );
    }

    // CallAsync: run a command's pipeline for side effects, preserve input

    public static IPipelineBuilder<TStart, TOutput> CallAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ICommandProcedure<TOutput> command,
        string name
    )
    {
        return parent.CallAsync( command.PipelineFunction, name );
    }

    public static IPipelineBuilder<TStart, TOutput> CallAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        ICommandProcedure<TOutput> command,
        Action<IPipelineContext> config = null
    )
    {
        return parent.CallAsync( command.PipelineFunction, config );
    }

    // PipeIf: conditionally transform value through a command's pipeline

    public static IPipelineBuilder<TStart, TNext> PipeIf<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        ICommandFunction<TOutput, TNext> command
    )
    {
        ArgumentNullException.ThrowIfNull( command );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new PipeIfBlockBinder<TStart, TOutput>( condition, parentFunction )
                .Bind( command.PipelineFunction ),
            Middleware = parentMiddleware
        };
    }

    // CallIf: conditionally run a command's pipeline for side effects, preserve input

    public static IPipelineBuilder<TStart, TOutput> CallIf<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Function<TOutput, bool> condition,
        ICommandProcedure<TOutput> command
    )
    {
        ArgumentNullException.ThrowIfNull( command );
        ArgumentNullException.ThrowIfNull( condition );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        FunctionAsync<TOutput, object> wrappedFunction = async ( context, argument ) =>
        {
            await command.PipelineFunction( context, argument ).ConfigureAwait( false );
            return null;
        };

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new CallIfBlockBinder<TStart, TOutput>( condition, parentFunction )
                .Bind( wrappedFunction ),
            Middleware = parentMiddleware
        };
    }
}
