using Hyperbee.Pipeline.Binders.Abstractions;

using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( PipeBlockBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!
            .MakeGenericMethod( typeof( TNext ) );

        // Create parameters for the lambda expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        // Create a call expression to the helper method
        var callBindImplAsync = Expression.Call(
            Expression.Constant( this ),
            bindImplAsyncMethodInfo,
            next,
            Pipeline,
            paramContext,
            paramArgument
        );

        // Create and return the final expression
        return Expression.Lambda<FunctionAsync<TInput, TNext>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TNext> BindImplAsync<TNext>(
        FunctionAsync<TOutput, TNext> next,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument )
    {
        var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
    }
}
