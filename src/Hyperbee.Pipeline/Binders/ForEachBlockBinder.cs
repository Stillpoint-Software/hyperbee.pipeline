namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }

    public ForEachBlockBinder( FunctionAsync<TInput, TOutput> function )
    {
        Pipeline = function;
    }

    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TElement, object> next )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );
            var nextArguments = (IEnumerable<TElement>) nextArgument;

            foreach ( var elementArgument in nextArguments )
            {
                await next( context, elementArgument ).ConfigureAwait( false );
            }

            return nextArgument;
        };
    }
}
