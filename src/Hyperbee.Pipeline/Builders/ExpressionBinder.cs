using System.Linq.Expressions;

namespace Hyperbee.Pipeline;

public static class ExpressionBinder
{
    public static Expression<FunctionAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( FunctionAsync<TInput, TOutput> function ) =>
        ( context, input ) => function( context, input );

    public static Expression<ProcedureAsync<TInput>> ToExpression<TInput>( ProcedureAsync<TInput> function ) =>
        ( context, argument ) => function( context, argument );

    public static Expression<MiddlewareAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( MiddlewareAsync<TInput, TOutput> middleware ) =>
        ( context, argument, next ) => middleware( context, argument, next );
}
