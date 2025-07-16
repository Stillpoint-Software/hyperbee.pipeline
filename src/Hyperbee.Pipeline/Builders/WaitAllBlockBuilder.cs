using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public delegate TOutput WaitAllReducer<in TStart, out TOutput>( IPipelineContext context, TStart input, WaitAllResult[] results );

public static class WaitAllBlockBuilder
{
    public static IPipelineBuilder<TStart, TNext> WaitAll<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TStart, TOutput>.WaitAll( parent, true, builders, reducer, config );
    }

    public static IPipelineBuilder<TStart, TNext> WaitAll<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TStart, TOutput>.WaitAll( parent, inheritMiddleware, builders, reducer, config );
    }

    public static IPipelineBuilder<TStart, TOutput> WaitAll<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TStart, TOutput>.WaitAll( parent, true, builders, config );
    }

    public static IPipelineBuilder<TStart, TOutput> WaitAll<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TStart, TOutput>.WaitAll( parent, inheritMiddleware, builders, config );
    }
}

internal static class WaitAllBlockBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TNext> WaitAll<TNext>(
        IPipelineBuilder<TStart, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, true, builders, reducer, config );
    }

    public static IPipelineBuilder<TStart, TOutput> WaitAll(
        IPipelineBuilder<TStart, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, true, builders, config );
    }

    public static IPipelineBuilder<TStart, TOutput> WaitAll(
        IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, inheritMiddleware, builders, DefaultReducer, config );

        // create a default reducer that returns the arg from the previous step
        static TOutput DefaultReducer( IPipelineContext ctx, TOutput arg, WaitAllResult[] results ) => arg;
    }

    public static IPipelineBuilder<TStart, TNext> WaitAll<TNext>(
        IPipelineBuilder<TStart, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        ArgumentNullException.ThrowIfNull( builders );

        var builderInstances = builders( new Builders<TOutput, TOutput>() );

        if ( builderInstances.Length == 0 )
            throw new ArgumentOutOfRangeException( nameof( builders ) );

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        var functions = builderInstances
            .Select( builder => new { builder, block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null ) } )
            .Select( x => x.builder( x.block ).CastFunction<TOutput, object>() )
            .ToArray();

        return new PipelineBuilder<TStart, TNext>
        {
            Function = new WaitAllBlockBinder<TStart, TOutput>( parentFunction, parentMiddleware, config ).Bind( functions, reducer ),
            Middleware = parentMiddleware
        };
    }
}

public class Builders<TStart, TOutput>
{
    // convenience function to build an array of builder funcs. we don't want to require the 
    // builder array to be the last argument because it would force a signature pattern that
    // runs counter to our existing conventions. this constraint prevents us from using params.
    // a side effect of this is that while params handles generic type inference nicely, pure
    // array usages do not. straight up array usage would require the user to declare the full
    // type in their builder steps which would be noisy and error-prone. we can side-step this
    // problem by introducing this helper.
    public Func<IPipelineStartBuilder<TStart, TOutput>, IPipelineBuilder>[] Create( params Func<IPipelineStartBuilder<TStart, TOutput>, IPipelineBuilder>[] builders ) => builders;
}

public sealed record WaitAllResult
{
    internal WaitAllResult()
    {
    }

    public object Result { get; init; }
    public IPipelineContext Context { get; init; }
}
