using Hyperbee.Pipeline.Binders.Abstractions;

using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( null, function, default )
    {
    }

    public PipeBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        var paramContext = Expression.Parameter( typeof( TInput ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        var invokePipeline = Expression.Invoke( Pipeline, paramContext, paramArgument );
        var invokeCondition = Condition != null 
            ? Expression.Invoke( Condition, invokePipeline ) 
            : (Expression) Expression.Constant( true );

        var invokeNext = Expression.Invoke( 
            ExpressionBinder.ToExpression( next ), 
            paramContext, 
            invokePipeline );

        var body = Expression.Condition( invokeCondition, invokeNext, Expression.Convert( invokePipeline, typeof( TNext ) ) );

        return Expression.Lambda<FunctionAsync<TInput, TNext>>( body, paramContext, paramArgument );
    }

}


/*

internal class PipeBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Function<TOutput, bool> Condition { get; }

    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
        : base( null, function, default )
    {
    }

    public PipeBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
        };
    }
}
*/

