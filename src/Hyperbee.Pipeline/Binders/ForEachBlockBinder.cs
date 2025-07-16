using Hyperbee.Pipeline.Binders.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TStart, TOutput, TElement> : BlockBinder<TStart, TOutput>
{
    public ForEachBlockBinder( FunctionAsync<TStart, TOutput> function )
        : base( function, default )
    {
    }

    public FunctionAsync<TStart, TOutput> Bind( FunctionAsync<TElement, object> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            if (nextArgument is IList<TElement> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;
                    await ProcessBlockAsync(next, context, list[i]).ConfigureAwait(false);
                }
            }
            else if (nextArgument is TElement[] array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;
                    await ProcessBlockAsync(next, context, array[i]).ConfigureAwait(false);
                }
            }
            else if (nextArgument is IEnumerable<TElement> enumerable)
            {
                foreach (var elementArgument in enumerable)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;

                    await ProcessBlockAsync(next, context, elementArgument).ConfigureAwait(false);
                }
            }

            return nextArgument;
        };
    }
}

