//#define FastExpressionCompiler
using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Data;
using static System.Linq.Expressions.Expression;

using static Hyperbee.Expressions.ExpressionExtensions;

#if FastExpressionCompiler
using FastExpressionCompiler;
using static FastExpressionCompiler.ExpressionCompiler;
#endif

namespace Hyperbee.Pipeline;

public class PipelineBuilder<TInput, TOutput> : PipelineFactory, IPipelineStartBuilder<TInput, TOutput>, IPipelineFunctionProvider<TInput, TOutput>
{
    internal Expression<FunctionAsync<TInput, TOutput>> Function { get; init; }
    internal Expression<MiddlewareAsync<object, object>> Middleware { get; init; }

    internal PipelineBuilder()
    {
    }

    public FunctionAsync<TInput, TOutput> Build()
    {
        // build and return the outermost method
#if FastExpressionCompiler
        var code = Function.ToCSharpString();
        var compiledFunction = Function.CompileFast( false, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression );
        var di = compiledFunction.Target as IDelegateDebugInfo;
#else
        var compiledFunction = Function.Compile();
#endif

        return async ( context, argument ) =>
        {
            try
            {
                var result = await compiledFunction( context, argument ).ConfigureAwait( false );

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
#if FastExpressionCompiler
        //var code = Function.ToCSharpString();
        var compiledFunction = Function.CompileFast( false, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression );
#else
        var compiledFunction = Function.Compile();
#endif

        return async ( context, argument ) =>
        {
            try
            {
                await compiledFunction( context, argument ).ConfigureAwait( false );
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
        var compiledFunction = Function.Compile();

        return async ( context, argument ) =>
        {
            var result = await compiledFunction( context, Cast<TInput>( argument ) ).ConfigureAwait( false );
            return Cast<TOut>( result );
        };

        static TType Cast<TType>( object value ) => (TType) value;
    }

    Expression<FunctionAsync<TIn, TOut>> IPipelineBuilder.CastExpression<TIn, TOut>()
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        return Lambda<FunctionAsync<TIn, TOut>>(
            BlockAsync(
                Convert( Await( Invoke( Function, context, Convert( argument, typeof( TInput ) ) ), configureAwait: false ), typeof( TOut ) )
            ),
            parameters: [context, argument]
        );
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
        public Expression<FunctionAsync<TInput, TOutput>> Function { get; init; }
        public Expression<MiddlewareAsync<object, object>> Middleware { get; init; }
    }
}
