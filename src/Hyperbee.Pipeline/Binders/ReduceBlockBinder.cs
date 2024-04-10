namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Func<TNext, TNext, TNext> Reducer { get; }

    public ReduceBlockBinder( Func<TNext, TNext, TNext> reducer, FunctionAsync<TInput, TOutput> function )
    {
        Reducer = reducer;
        Pipeline = function;
    }

    public FunctionAsync<TInput, TNext> Bind( FunctionAsync<TElement, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );
            var nextArguments = (IEnumerable<TElement>) nextArgument;

            var accumulator = default( TNext );

            foreach ( var elementArgument in nextArguments )
            {
                var result = await next( context, elementArgument ).ConfigureAwait( false );
                accumulator = Reducer( accumulator, result );
            }

            return accumulator;
        };
    }
}
