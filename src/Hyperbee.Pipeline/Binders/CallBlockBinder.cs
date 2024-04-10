namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Function<TOutput, bool> Condition { get; }

    public CallBlockBinder( FunctionAsync<TInput, TOutput> function )
        : this( null, function )
    {
    }

    public CallBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
    {
        Condition = condition;
        Pipeline = function;
    }

    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TOutput, object> next )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

            if ( Condition == null || Condition( context, nextArgument ) )
                await next( context, nextArgument ).ConfigureAwait( false );

            return nextArgument;
        };
    }
}
