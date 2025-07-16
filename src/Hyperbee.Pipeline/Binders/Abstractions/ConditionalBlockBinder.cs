using System.Runtime.CompilerServices;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class ConditionalBlockBinder<TStart, TOutput> : BlockBinder<TStart, TOutput>
{
    protected Function<TOutput, bool> Condition { get; }

    protected ConditionalBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TStart, TOutput> function, Action<IPipelineContext> configure )
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
