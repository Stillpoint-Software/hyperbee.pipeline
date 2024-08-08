using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    private Expression<Function<TOutput, bool>> Condition { get; }

    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : this( null, function )
    {
    }

    public PipeBlockBinder( Function<TOutput, bool> condition, Expression<FunctionAsync<TInput, TOutput>> function )
    {
        Condition = ConvertCondition( condition );
        Pipeline = function;
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        var paramContext = Expression.Parameter( typeof( TInput ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        var invokePipeline = Expression.Invoke( Pipeline, paramContext, paramArgument );
        var invokeCondition = Condition != null 
            ? Expression.Invoke( Condition, invokePipeline ) 
            : (Expression) Expression.Constant( true );

        var invokeNext = Expression.Invoke( next, paramContext, invokePipeline );

        var body = Expression.Condition( invokeCondition, invokeNext, Expression.Convert( invokePipeline, typeof( TNext ) ) );

        return Expression.Lambda<FunctionAsync<TInput, TNext>>( body, paramContext, paramArgument );
    }

    internal static Expression<Function<TOutput, bool>> ConvertCondition( Function<TOutput, bool> del )
    {
        // Get the MethodInfo of the delegate
        var methodInfo = del.GetMethodInfo();

        // Create a parameter expression
        var parameter = Expression.Parameter( typeof( TInput ), "input" );

        // Create a method call expression
        var methodCall = Expression.Call( Expression.Constant( del.Target ), methodInfo, parameter );

        // Create and return the lambda expression
        return Expression.Lambda<Function<TOutput, bool>>( methodCall, parameter );
    }


}


/*

internal class PipeBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Function<TOutput, bool> Condition { get; }

    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
        : this( null, function )
    {
    }

    public PipeBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
    {
        Condition = condition;
        Pipeline = function;
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

            if ( Condition == null || Condition( context, nextArgument ) )
                return await next( context, nextArgument ).ConfigureAwait( false );

            return (TNext) (object) nextArgument;
        };
    }
}
*/
