﻿using Hyperbee.Pipeline.Data;

namespace Hyperbee.Pipeline;

public class PipelineBuilder<TInput, TOutput> : PipelineFactory, IPipelineStartBuilder<TInput, TOutput>, IPipelineFunctionProvider<TInput, TOutput>
{
    internal FunctionAsync<TInput, TOutput> Function { get; init; }
    internal MiddlewareAsync<object, object> Middleware { get; init; }

    internal PipelineBuilder()
    {
    }

    public FunctionAsync<TInput, TOutput> Build()
    {
        // build and return the outermost method
        return async ( context, argument ) =>
        {
            try
            {
                var result = await Function( context, argument ).ConfigureAwait( false );

                if ( context.CancellationToken.IsCancellationRequested )
                    return Converter.TryConvertTo<TOutput>( context.CancellationValue, out var converted ) ? converted : default;

                return result;
            }
            catch ( Exception ex )
            {
                context.Exception = ex;

                if ( context.Throws )
                    throw;
            }

            return default;
        };
    }

    public ProcedureAsync<TInput> BuildAsProcedure()
    {
        // build and return the outermost method
        return async ( context, argument ) =>
        {
            try
            {
                await Function( context, argument ).ConfigureAwait( false );
            }
            catch ( Exception ex )
            {
                context.Exception = ex;

                if ( context.Throws )
                    throw;
            }
        };
    }

    FunctionAsync<TIn, TOut> IPipelineBuilder.CastFunction<TIn, TOut>()
    {
        return async ( context, argument ) =>
        {
            var result = await Function( context, Cast<TInput>( argument ) ).ConfigureAwait( false );
            return Cast<TOut>( result );
        };

        static TType Cast<TType>( object value ) => (TType) value;
    }

    // custom builders and binders need access to Function and Middleware
    IPipelineFunction<TInput, TOutput> IPipelineFunctionProvider<TInput, TOutput>.GetPipelineFunction()
    {
        return new PipelineFunction
        {
            Function = Function,
            Middleware = Middleware
        };
    }

    public record PipelineFunction : IPipelineFunction<TInput, TOutput>
    {
        public FunctionAsync<TInput, TOutput> Function { get; init; }
        public MiddlewareAsync<object, object> Middleware { get; init; }
    }
}
