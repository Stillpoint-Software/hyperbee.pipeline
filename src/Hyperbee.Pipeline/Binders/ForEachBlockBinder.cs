using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement> : BlockBinder<TInput, TOutput>
{
    public ForEachBlockBinder( FunctionAsync<TInput, TOutput> function )
        : base( function, default )
    {
    }

    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TElement, object> next )

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

