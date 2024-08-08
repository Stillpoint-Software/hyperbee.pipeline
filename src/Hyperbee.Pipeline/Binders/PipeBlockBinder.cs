using Hyperbee.Pipeline.Binders.Abstractions;

using System.Linq.Expressions;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        var paramContext = Expression.Parameter( typeof( TInput ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        var invokePipeline = Expression.Invoke( Pipeline, paramContext, paramArgument );

        var invokeNext = Expression.Invoke( 
            next, 
            paramContext, 
            invokePipeline );

        return Expression.Lambda<FunctionAsync<TInput, TNext>>( invokeNext, paramContext, paramArgument );
    }

}


/*

internal class PipeBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Function<TOutput, bool> Condition { get; }

    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
        : base( function, default )
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
