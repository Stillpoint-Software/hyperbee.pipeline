using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext> : BlockBinder<TInput, TOutput>
{
    private Func<TNext, TNext, TNext> Reducer { get; }

    public ReduceBlockBinder( Func<TNext, TNext, TNext> reducer, FunctionAsync<TInput, TOutput> function )
        : base( function, default )
    {
        Reducer = reducer;
    }

    public FunctionAsync<TInput, TNext> Bind( FunctionAsync<TElement, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            var nextArguments = (IEnumerable<TElement>) nextArgument;
            var accumulator = default( TNext );

            // Process each element and apply the reducer
            foreach ( var elementArgument in nextArguments )
            {
                var result = await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
                accumulator = Reducer( accumulator, result );
            }

            return accumulator;
        };
    }
}

