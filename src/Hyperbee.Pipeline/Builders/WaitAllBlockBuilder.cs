using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public delegate TOutput WaitAllReducer<in TInput, out TOutput>( IPipelineContext context, TInput input, WaitAllResult[] results );

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TNext> WaitAll<TNext>( Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders, WaitAllReducer<TOutput, TNext> reducer, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TNext> WaitAll<TNext>( bool inheritMiddleware, Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders, WaitAllReducer<TOutput, TNext> reducer, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TOutput> WaitAll( Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TOutput> WaitAll( bool inheritMiddleware, Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders, Action<IPipelineContext> config = null );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TNext> WaitAll<TNext>(
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( true, builders, reducer, config );
    }

    public IPipelineBuilder<TInput, TOutput> WaitAll(
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>,
                IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( true, builders, config );
    }

    public IPipelineBuilder<TInput, TOutput> WaitAll(
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>,
            Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( true, builders, DefaultReducer, config );

        // create a default reducer that returns the arg from the previous step
        static TOutput DefaultReducer( IPipelineContext ctx, TOutput arg, WaitAllResult[] results ) => arg;
    }

    public IPipelineBuilder<TInput, TNext> WaitAll<TNext>(
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( builders );

        var builderInstances = builders( new Builders<TOutput, TOutput>() );

        if ( builderInstances.Length == 0 )
            throw new ArgumentOutOfRangeException( nameof( builders ) );

        var functions = builderInstances
            .Select( builder => new { builder, block = PipelineFactory.Start<TOutput>( inheritMiddleware ? Middleware : null ) } )
            .Select( x => x.builder( x.block ).CastFunction<TOutput, object>() )
            .ToArray();

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new WaitAllBlockBinder<TInput, TOutput>( Function, Middleware, config ).Bind( functions, reducer ),
            Middleware = Middleware
        };
    }
}

public class Builders<TInput, TOutput>
{
    // convenience function to build an array of builder funcs. we don't want to require the 
    // builder array to be the last argument because it would force a signature pattern that
    // runs counter to our existing conventions. this constraint prevents us from using params.
    // a side effect of this is that while params handles generic type inference nicely, pure
    // array usages do not. straight up array usage would require the user to declare the full
    // type in their builder steps which would be noisy and error-prone. we can side-step this
    // problem by introducing this helper.
    public Func<IPipelineStartBuilder<TInput, TOutput>, IPipelineBuilder>[] Create( params Func<IPipelineStartBuilder<TInput, TOutput>, IPipelineBuilder>[] builders ) => builders;
}

public sealed record WaitAllResult
{
    internal WaitAllResult()
    {
    }

    public object Result { get; init; }
    public IPipelineContext Context { get; init; }
}
