using Hyperbee.Pipeline.Binders.Abstractions;

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

            var nextArguments = (IEnumerable<TElement>) nextArgument;

            foreach ( var elementArgument in nextArguments )
            {
                await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
            }

            return nextArgument;
        };
    }
}

