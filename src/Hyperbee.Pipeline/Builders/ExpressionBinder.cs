using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public static class ExpressionBinder
{
    public static Expression<Action<IPipelineContext>> ToExpression( Action<IPipelineContext> action )
    {
        if ( action == null )
            return null;

        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );

        return Expression.Lambda<Action<IPipelineContext>>(
            Expression.Invoke( Expression.Constant( action ), contextParameter ),
            contextParameter
        );
    }

    public static Expression<Func<TInput, TOutput>> ToExpression<TInput, TOutput>( Func<TInput, TOutput> func )
    {
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );

        return Expression.Lambda<Func<TInput, TOutput>>(
            Expression.Invoke( Expression.Constant( func ), inputParameter ),
            inputParameter
        );
    }

    public static Expression<Function<TInput, TOutput>> ToExpression<TInput, TOutput>( Function<TInput, TOutput> function )
    {
        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );

        return Expression.Lambda<Function<TInput, TOutput>>(
            Expression.Invoke( Expression.Constant( function ), contextParameter, inputParameter ),
            contextParameter, 
            inputParameter
        );
    }

    public static Expression<Procedure<TInput>> ToExpression<TInput>( Procedure<TInput> function )
    {
        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );

        return Expression.Lambda<Procedure<TInput>>(
            Expression.Invoke( Expression.Constant( function ), contextParameter, inputParameter ),
            contextParameter,
            inputParameter
        );
    }

    public static Expression<FunctionAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( FunctionAsync<TInput, TOutput> function )
    {
        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );

        return Expression.Lambda<FunctionAsync<TInput, TOutput>>(
            Expression.Invoke( Expression.Constant( function ), contextParameter, inputParameter ),
            contextParameter,
            inputParameter
        );
    }

    public static Expression<ProcedureAsync<TInput>> ToExpression<TInput>( ProcedureAsync<TInput> function )
    {
        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );

        return Expression.Lambda<ProcedureAsync<TInput>>(
            Expression.Invoke( Expression.Constant( function ), contextParameter, inputParameter ),
            contextParameter,
            inputParameter
        );
    }

    public static Expression<MiddlewareAsync<TInput, TOutput>> ToExpression<TInput, TOutput>( MiddlewareAsync<TInput, TOutput> middleware )
    {
        var contextParameter = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var inputParameter = Expression.Parameter( typeof( TInput ), "input" );
        var nextParameter = Expression.Parameter( typeof( FunctionAsync<TInput, TOutput> ), "next" );

        return Expression.Lambda<MiddlewareAsync<TInput, TOutput>>(
            Expression.Invoke( Expression.Constant( middleware ), contextParameter, inputParameter, nextParameter ),
            contextParameter,
            inputParameter,
            nextParameter
        );
    }
}
