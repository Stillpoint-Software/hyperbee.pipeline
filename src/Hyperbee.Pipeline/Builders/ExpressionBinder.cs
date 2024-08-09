using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public static class ExpressionBinder
{
    public static Expression<Action<IPipelineContext>> ToExpression( Action<IPipelineContext> action )
    {
        if ( action == null )
            return null;

        return (context) => action(context);
    }

    public static Expression<Func<TInput, TOutput>> ToExpression<TInput, TOutput>( Func<TInput, TOutput> func ) => 
        input => func( input );

    public static Expression<Function<TInput, TOutput>> ToExpression<TInput, TOutput>( Function<TInput, TOutput> function ) => 
        (context, argument) => function(context, argument);

    public static Expression<Procedure<TInput>> ToExpression<TInput>( Procedure<TInput> function ) => 
        (context, argument) => function(context, argument);

    public static Expression<FunctionAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( FunctionAsync<TInput, TOutput> function ) => 
        ( context, input ) => function( context, input );

    public static Expression<ProcedureAsync<TInput>> ToExpression<TInput>( ProcedureAsync<TInput> function ) => 
        (context, argument) => function(context, argument);

    public static Expression<MiddlewareAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( MiddlewareAsync<TInput, TOutput> middleware ) => 
        (context, argument, next) => middleware(context, argument, next);
}
