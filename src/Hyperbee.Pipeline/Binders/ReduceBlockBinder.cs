using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TStart, TOutput, TElement, TNext> : BlockBinder<TStart, TOutput>
{
    private Func<TNext, TNext, TNext> Reducer { get; }

    public ReduceBlockBinder( Func<TNext, TNext, TNext> reducer, FunctionAsync<TStart, TOutput> function )
        : base( function, default )
    {
        Reducer = reducer;
    }

    public FunctionAsync<TStart, TNext> Bind( FunctionAsync<TElement, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            TNext accumulator = default;
            if ( nextArgument is IList<TElement> list )
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    if ( context.CancellationToken.IsCancellationRequested )
                        break;
                    var result = await ProcessBlockAsync( next, context, list[i] ).ConfigureAwait( false );
                    accumulator = Reducer( accumulator, result );
                }
            }
            else if ( nextArgument is TElement[] array )
            {
                for ( int i = 0; i < array.Length; i++ )
                {
                    if ( context.CancellationToken.IsCancellationRequested )
                        break;
                    var result = await ProcessBlockAsync( next, context, array[i] ).ConfigureAwait( false );
                    accumulator = Reducer( accumulator, result );
                }
            }
            else if ( nextArgument is IEnumerable<TElement> enumerable )
            {
                foreach ( var elementArgument in enumerable )
                {
                    if ( context.CancellationToken.IsCancellationRequested )
                        break;

                    var result = await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
                    accumulator = Reducer( accumulator, result );
                }
            }

            return accumulator;
        };
    }
}

