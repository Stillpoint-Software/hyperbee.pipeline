using System.Runtime.CompilerServices;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal abstract class Binder<TInput, TOutput>
{
    protected FunctionAsync<TInput, TOutput> Pipeline { get; }
    protected Action<IPipelineContext> Configure { get; }

    protected Binder( FunctionAsync<TInput, TOutput> function, Action<IPipelineContext> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    protected virtual async Task<(TOutput Result, bool Canceled)> ProcessPipelineAsync( IPipelineContext context, TInput argument )
    {
        var result = await Pipeline( context, argument ).ConfigureAwait( false );

        var contextControl = (IPipelineContextControl) context;
        var canceled = contextControl.HandleCancellationRequested( result );

        return (canceled ? default : result, canceled);
    }
}

internal abstract class StatementBinder<TInput, TOutput> : Binder<TInput, TOutput>
{
    protected MiddlewareAsync<object, object> Middleware { get; }

    protected StatementBinder( FunctionAsync<TInput, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, configure )
    {
        Middleware = middleware;
    }

    protected virtual async Task<TNext> ProcessStatementAsync<TNext>( FunctionAsync<TOutput, TNext> nextFunction, IPipelineContext context, TOutput nextArgument, string frameName )
    {
        var contextControl = (IPipelineContextControl) context;

        using var _ = contextControl.CreateFrame( context, Configure, frameName );

        if ( Middleware == null )
            return await nextFunction( context, nextArgument ).ConfigureAwait( false );

        return (TNext) await Middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await nextFunction( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}

internal abstract class BlockBinder<TInput, TOutput> : Binder<TInput, TOutput>
{
    protected BlockBinder( FunctionAsync<TInput, TOutput> function, Action<IPipelineContext> configure )
        : base( function, configure )
    {
    }

    // Using TArgument instead of TOutput allows more capabilities for special
    // use cases where the next argument is not the same as the output type
    // like ReduceBlockBinder and ForEachBlockBinder

    protected virtual async Task<TNext> ProcessBlockAsync<TArgument, TNext>( FunctionAsync<TArgument, TNext> blockFunction, IPipelineContext context, TArgument nextArgument )
    {
        return await blockFunction( context, nextArgument ).ConfigureAwait( false );
    }
}

internal abstract class ConditionalBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    protected Function<TOutput, bool> Condition { get; }

    protected ConditionalBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function, Action<IPipelineContext> configure )
        : base( function, configure )
    {
        Condition = condition;
    }

    protected override async Task<TNext> ProcessBlockAsync<TArgument, TNext>( FunctionAsync<TArgument, TNext> blockFunction, IPipelineContext context, TArgument nextArgument )
    {
        if ( Condition != null && !Condition( context, CastTypeArg<TArgument, TOutput>( nextArgument ) ) )
        {
            return CastTypeArg<TArgument, TNext>( nextArgument );
        }

        return await base.ProcessBlockAsync( blockFunction, context, nextArgument ).ConfigureAwait( false );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static TResult CastTypeArg<TType, TResult>( TType input )
    {
        return (TResult) (object) input;
    }
}
