using Hyperbee.Pipeline.Data;

namespace Hyperbee.Pipeline;

public class PipelineBuilder<TStart, TOutput> : PipelineFactory, IPipelineStartBuilder<TStart, TOutput>, IPipelineFunctionProvider<TStart, TOutput>
{
    internal FunctionAsync<TStart, TOutput> Function { get; init; }
    internal MiddlewareAsync<object, object> Middleware { get; init; }

    internal PipelineBuilder()
    {
    }

    public FunctionAsync<TStart, TOutput> Build()
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

    public ProcedureAsync<TStart> BuildAsProcedure()
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
            var result = await Function( context, Cast<TStart>( argument ) ).ConfigureAwait( false );
            return Cast<TOut>( result );
        };

        static TType Cast<TType>( object value ) => (TType) value;
    }

    // custom builders and binders need access to Function and Middleware
    IPipelineFunction<TStart, TOutput> IPipelineFunctionProvider<TStart, TOutput>.GetPipelineFunction()
    {
        return new PipelineFunction
        {
            Function = Function,
            Middleware = Middleware
        };
    }

    public record PipelineFunction : IPipelineFunction<TStart, TOutput>
    {
        public FunctionAsync<TStart, TOutput> Function { get; init; }
        public MiddlewareAsync<object, object> Middleware { get; init; }
    }
}
