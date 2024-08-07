namespace Hyperbee.Pipeline.Binders;

//internal class PipeBlockBinder<TInput, TOutput>
//{
//    private FunctionAsync<TInput, TOutput> Pipeline { get; }
//    private Function<TOutput, bool> Condition { get; }

//    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
//        : this( null, function )
//    {
//    }

//    public PipeBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
//    {
//        Condition = condition;
//        Pipeline = function;
//    }

//    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
//    {
//        return async ( context, argument ) =>
//        {
//            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

//            if ( Condition == null || Condition( context, nextArgument ) )
//                return await next( context, nextArgument ).ConfigureAwait( false );

//            return (TNext) (object) nextArgument;
//        };
//    }
//}

internal class PipeBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( FunctionAsync<TInput, TOutput> function )
        : base( null, function, default )
    {
    }

    public PipeBlockBinder( Function<TOutput, bool> condition, FunctionAsync<TInput, TOutput> function )
        : base( condition, function, default )
    {
    }

    public FunctionAsync<TInput, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
        };
    }
}

