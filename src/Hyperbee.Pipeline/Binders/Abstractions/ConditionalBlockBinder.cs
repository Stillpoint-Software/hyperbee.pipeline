using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class ConditionalBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    protected Expression<Function<TOutput, bool>> Condition { get; }

    protected ConditionalBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function, Expression<Action<IPipelineContext>> configure )
        : base( function, configure )
    {
        Condition = condition;
    }

    // protected override async Task<TNext> ProcessBlockAsync<TArgument, TNext>( FunctionAsync<TArgument, TNext> blockFunction, IPipelineContext context, TArgument nextArgument )
    // {
    //     if ( Condition != null && !Condition( context, CastTypeArg<TArgument, TOutput>( nextArgument ) ) )
    //     {
    //         return CastTypeArg<TArgument, TNext>( nextArgument );
    //     }
    //
    //     return await base.ProcessBlockAsync( blockFunction, context, nextArgument ).ConfigureAwait( false );
    // }

    // [MethodImpl( MethodImplOptions.AggressiveInlining )]
    // private static TResult CastTypeArg<TType, TResult>( TType input )
    // {
    //     return (TResult) (object) input;
    // }

    protected override Expression<Task<TNext>> ProcessBlockAsync<TArgument, TNext>(
        Expression<FunctionAsync<TArgument, TNext>> blockFunction,
        ParameterExpression context,
        Expression nextArgument )
    {
        if ( Condition == null )
            return base.ProcessBlockAsync( blockFunction, context, nextArgument );

        var nextArgumentExpression = Constant( nextArgument );

        return Lambda<Task<TNext>>(
            IfThenElse(
                Not( Invoke( Condition, Constant( context ),
                    Convert( Convert( nextArgumentExpression, typeof( object ) ), typeof( TOutput ) ) ) ),
                Convert( Convert( Constant( nextArgument ), typeof( object ) ), typeof( TNext ) ),
                base.ProcessBlockAsync( blockFunction, context, nextArgument )
            ) );
    }
}
