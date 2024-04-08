namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private Function<TOutput, bool> Condition { get; }

    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
        : this( null, function )
    {
    }

    public PipeBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
    {
        Condition = condition;
        Pipeline = function;
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

            if ( Condition == null || Condition( context, nextArgument ) )
                return await next( context, nextArgument ).ConfigureAwait( false );

            return (TNext) (object) nextArgument;
        };
    }
}