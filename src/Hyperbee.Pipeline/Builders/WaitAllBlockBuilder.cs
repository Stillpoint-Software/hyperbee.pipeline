using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public delegate TOutput WaitAllReducer<in TInput, out TOutput>( IPipelineContext context, TInput input, WaitAllResult[] results );

public static class WaitAllBlockBuilder
{
    public static IPipelineBuilder<TInput, TNext> WaitAll<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TInput, TOutput>.WaitAll( parent, true, builders, reducer, config );
    }

    public static IPipelineBuilder<TInput, TNext> WaitAll<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TInput, TOutput>.WaitAll( parent, inheritMiddleware, builders, reducer, config );
    }

    public static IPipelineBuilder<TInput, TOutput> WaitAll<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TInput, TOutput>.WaitAll( parent, true, builders, config );
    }

    public static IPipelineBuilder<TInput, TOutput> WaitAll<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAllBlockBuilder<TInput, TOutput>.WaitAll( parent, inheritMiddleware, builders, config );
    }
}

internal static class WaitAllBlockBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TNext> WaitAll<TNext>(
        IPipelineBuilder<TInput, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        WaitAllReducer<TOutput, TNext> reducer,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, true, builders, reducer, config );
    }

    public static IPipelineBuilder<TInput, TOutput> WaitAll(
        IPipelineBuilder<TInput, TOutput> parent,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, true, builders, config );
    }

    public static IPipelineBuilder<TInput, TOutput> WaitAll(
        IPipelineBuilder<TInput, TOutput> parent,
        bool inheritMiddleware,
        Func<Builders<TOutput, TOutput>, Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder>[]> builders,
        Action<IPipelineContext> config = null )
    {
        return WaitAll( parent, inheritMiddleware, builders, DefaultReducer, config );

        // create a default reducer that returns the arg from the previous step
        static TOutput DefaultReducer( IPipelineContext ctx, TOutput arg, WaitAllResult[] results ) => arg;
    }

    public static IPipelineBuilder<TInput, TNext> WaitAll<TNext>(
        IPipelineBuilder<TInput, TOutput> parent,
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

        var functions = Expression.Constant( builderInstances
            .Select( builder => new { builder, block = PipelineFactory.Start<TOutput>( inheritMiddleware ? parentMiddleware : null ) } )
            .Select( x => x.builder( x.block ).CastFunction<TOutput, object>() )
            .ToArray() );

        Expression<Action<IPipelineContext>> configExpression = config == null
            ? null
            : ctx => config( ctx );

        Expression<WaitAllReducer<TOutput, TNext>> reducerExpression = ( ctx, arg, results ) => reducer( ctx, arg, results );

        return new PipelineBuilder<TInput, TNext>
        {
            Function = new WaitAllBlockBinder<TInput, TOutput>( parentFunction, parentMiddleware, configExpression ).Bind( functions, reducerExpression ),
            Middleware = parentMiddleware
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

public sealed record WaitAllResult( IPipelineContext Context, object Result );
//{
//    public WaitAllResult()
//    {
//    }

//    public object Result { get; init; }
//    public IPipelineContext Context { get; init; }
//}
