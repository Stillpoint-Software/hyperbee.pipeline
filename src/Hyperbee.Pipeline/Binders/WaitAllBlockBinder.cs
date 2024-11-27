using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;


internal class WaitAllBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    private Expression<MiddlewareAsync<object, object>> Middleware { get; }

    public WaitAllBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : this( null, function, middleware, configure )
    {
    }

    public WaitAllBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( condition, function, configure )
    {
        Middleware = middleware;
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>(
        Expression nexts,
        Expression<WaitAllReducer<TOutput, TNext>> reducer )
    {
        ArgumentNullException.ThrowIfNull( reducer );

        /*
        {
            return async ( context, argument ) =>
            {
                var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

                if ( canceled )
                    return default;

                // WaitAllBlockBinder is unique in that it is both a block configure and a step.
                // The reducer is the step action, and because it is a step, we need to ensure
                // that middleware is called. Middleware requires us to pass in the execution
                // function that it wraps. This requires an additional level of wrapping.

                return await WaitAllAsync( context, nextArgument, nexts, reducer ).ConfigureAwait( false );
            };
        }
        */

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),
                Condition( canceled,
                    Default( typeof( TNext ) ),
                    Await( WaitAllAsync( context, nextArgument, nexts, reducer ), configureAwait: false )
                )
            ),
            parameters: [context, argument]
        );
    }

    private Expression WaitAllAsync<TNext>(
        ParameterExpression context,
        Expression nextArgument,
        Expression nexts,  // FunctionAsync<TOutput, object>[] 
        Expression<WaitAllReducer<TOutput, TNext>> reducer )
    {
        /*
        {
            var contextControl = (IPipelineContextControl) context;
            using var _ = contextControl.CreateFrame( context, Configure, nameof( WaitAllAsync ) );

            var results = new WaitAllResult[nexts.Length];
            var items = nexts.Select( ( x, i ) => new { next = x, index = i } );

            await items.ForEachAsync( async item =>
            {
                var innerContext = context.Clone( false ); // context fork

                var result = await ProcessStatementAsync( item.next, innerContext, nextArgument ).ConfigureAwait( false );

                results[item.index] = new WaitAllResult { Context = innerContext, Result = result };
            } ).ConfigureAwait( false );

            return reducer( context, nextArgument, results );
        }
        */

        var results = Variable( typeof( WaitAllResult[] ), "results" );
        var result = Variable( typeof( TNext ), "result" );
        var innerContext = Variable( typeof( IPipelineContext ), "innerContext" );

        var indexedItem = Parameter( typeof( (FunctionAsync<TOutput, object>, int) ), "indexedItem" );
        var item = Field( indexedItem, "Item1" );
        var index = Field( indexedItem, "Item2" );

        var methodInfo = typeof( IPipelineContext ).GetMethod( nameof( IPipelineContext.Clone ) )!;

        var forEachBody = Lambda<Func<(FunctionAsync<TOutput, object>, int), Task>>(
            BlockAsync(
                [innerContext, result],

                Assign( innerContext, Call( context, methodInfo, [Constant( false )] ) ),

                Assign( result, Await( ProcessStatementAsync<TNext>( item, innerContext, nextArgument, "NAME" ), configureAwait: false ) ),
                Assign( ArrayAccess( results, index ), New( typeof( WaitAllResult ).GetConstructors()[0], innerContext, Convert( result, typeof( object ) ) ) )

            ),
            parameters: [indexedItem]
        );

        var items = Variable( typeof( IEnumerable<(FunctionAsync<TOutput, object>, int)> ), "items" );
        var innerForEach = ForEachAsync(
            items,
            forEachBody,
            Constant( Environment.ProcessorCount ) ); //Constant( 1 )

        var reducerResult = Variable( typeof( TNext ), "reducerResult" );

        // TODO: reducerResult shouldn't be required, using/block should return last expression
        return BlockAsync(
            [results, reducerResult],
            Using( //using var _ = contextControl.CreateFrame( context, Configure, frameName );
                ContextImplExtensions.CreateFrameExpression( context, Configure, nameof( WaitAllAsync ) ),
                Block(
                    [items],
                    Assign( results, NewArrayBounds( typeof( WaitAllResult ), ArrayLength( nexts ) ) ),
                    Assign( items, SelectIndexItem<FunctionAsync<TOutput, object>>( nexts ) ),

                    //Invoke( LoggerExpression.Log( "forEachBody.items" ), Convert( items, typeof( object ) ) ),

                    Await( innerForEach, configureAwait: false ),
                    Assign( reducerResult, Invoke( reducer, context, nextArgument, results ) )
                )
            ),
            reducerResult
        );
    }

    private Expression SelectIndexItem<T>( Expression nexts )
    {
        var elementParam = Parameter( typeof( T ), "next" );
        var indexParam = Parameter( typeof( int ), "index" );

        var tupleType = typeof( ValueTuple<T, int> );
        var selectMethod = typeof( Enumerable )
            .GetMethods()
            .First( m =>
                m.Name == nameof( Enumerable.Select ) &&
                m.GetParameters().Length == 2 && // Select with two parameters
                m.GetParameters()[1].ParameterType
                    .GetGenericTypeDefinition() == typeof( Func<,,> )
            )
            .MakeGenericMethod( typeof( T ), tupleType );

        // nexts.Select((next, index) => (next, index))
        return Call(
            selectMethod,
            nexts,
            Lambda(
                New(
                    tupleType.GetConstructor( [typeof( T ), typeof( int )] ),
                    elementParam,
                    indexParam
                ),
                elementParam,
                indexParam
            )
        );
    }

    protected virtual Expression ProcessStatementAsync<TNext>(
        Expression nextFunction,
        ParameterExpression context,
        Expression nextArgument,
        string frameName )
    {
        /*
        {
            if ( Middleware == null )
                return await nextFunction( context, nextArgument ).ConfigureAwait( false );

            return (TNext) await Middleware(
                context,
                nextArgument,
                async ( ctx, arg ) => await nextFunction( ctx, (TOutput) arg ).ConfigureAwait( false )
            ).ConfigureAwait( false );
        }
        */

        if ( Middleware == null )
            return BlockAsync(
                Convert(
                    Await( Invoke( nextFunction, context, nextArgument ), configureAwait: false ),
                    typeof( TNext ) )
                );

        // async ( ctx, arg ) => await nextFunction( ctx, (TOutput) arg ).ConfigureAwait( false )
        var ctx = Parameter( typeof( IPipelineContext ), "ctx" );
        var arg = Parameter( typeof( object ), "arg" );
        var middlewareNext = Lambda<FunctionAsync<object, object>>(
            BlockAsync(
                Convert(
                    Await( Invoke( nextFunction, ctx, Convert( arg, typeof( TOutput ) ) ), configureAwait: false ),
                    typeof( object ) )
            ),
            parameters: [ctx, arg]
        );

        return BlockAsync(
            Convert(
                Await(
                    Invoke( Middleware,
                        context,
                        Convert( nextArgument, typeof( object ) ),
                        middlewareNext
                    ),
                    configureAwait: false ),
                typeof( TNext ) )
        );
    }

    public static Expression ForEachAsync<TSource>(
        Expression sourceExpression,
        Expression<Func<(TSource, int), Task>> function,
        Expression partitionCount )
    {
        /*
        return Task.WhenAll( Partitioner
            .Create( source )
            .GetPartitions( maxDegreeOfParallelism )
            .Select( partition => Task.Run( async () =>
            {
                using var enumerator = partition;
                while ( partition.MoveNext() )
                {
                    await function( partition.Current ).ConfigureAwait( false );
                }
            } ) ) );
         */

        var createMethod = typeof( Partitioner )
            .GetMethods()
            .First( m => m.Name == nameof( Partitioner.Create ) &&
                m.IsGenericMethod &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof( IEnumerable<> ) );

        var createPartitionerCall = Call(
            createMethod.MakeGenericMethod( typeof( (TSource, int) ) ),
            sourceExpression );

        var orderablePartitionerType = typeof( OrderablePartitioner<> ).MakeGenericType( typeof( (TSource, int) ) );
        var getPartitionsMethod = orderablePartitionerType.GetMethod( nameof( OrderablePartitioner<(TSource, int)>.GetPartitions ) );
        var getPartitionsCall = Call(
            createPartitionerCall,
            getPartitionsMethod,
            partitionCount );  // returns IList<IEnumerator<(TSource, int)>>

        //var partition = Variable( typeof( IEnumerator<(TSource, int)> ), "partition" );
        var temp = Variable( typeof( int ), "_nh_temp" );
        var counter = Variable( typeof( int ), "counter" );

        var partitionList = Variable( typeof( IList<IEnumerator<(TSource, int)>> ), "partitionList" );
        var partitionAccess = MakeIndex( partitionList, typeof( IList<IEnumerator<(TSource, int)>> ).GetProperty( "Item" ), [temp] );
        var enumerator = Variable( typeof( IEnumerator<(TSource, int)> ), "enumerator" );

        // Task.Run( async () => {
        //      using var enumerator = partition;
        //      while ( enumerator.MoveNext() ) await function( enumerator.Current ) 
        // }
        var moveNext = Call( enumerator, typeof( IEnumerator ).GetMethod( nameof( IEnumerator.MoveNext ) ) );
        var current = Property( enumerator, nameof( IEnumerator<TSource>.Current ) );
        var taskRun = Call(
            typeof( Task ).GetMethod( nameof( Task.Run ), [typeof( Func<Task> )] )!,
            Lambda<Func<Task>>(
                BlockAsync(  // SM<11>
                    [enumerator],
                    Assign( enumerator, partitionAccess ),  // var enumerator = partitionList[temp]

                    //Invoke( LoggerExpression.Log( "taskRun.enumerator" ), Convert( enumerator, typeof( object ) ) ),
                    //Invoke( LoggerExpression.Log( "taskRun.counter" ), Convert( counter, typeof( object ) ) ),
                    //Invoke( LoggerExpression.Log( "taskRun.temp" ), Convert( temp, typeof( object ) ) ),

                    //Using( Convert( enumerator, typeof( IDisposable ) ),  // TODO: Shouldn't need to convert
                        While(
                            moveNext,
                            Await( Invoke( function, current ), configureAwait: false )
                        )
                    //)
                )
            )
        );

        /*
         * List<Task> tasks = new List<Task>();
         * foreach( var partition in partitions )
         * {
         *      tasks.Add( Task.Run( async () => { ... } ) ) );
         * }
         * for( int counter = 0; counter < 2: counter++ )
         * {
         *      
         *      await..  
         *      
         *      var temp = counter; ---
         *      tasks.Add( Task.Run( async () => { temp ... } ) ) );
         * }
         */
        var tasks = Variable( typeof( List<Task> ), "tasks" );
        var initalizeTasks = Assign( tasks, New( typeof( List<Task> ).GetConstructors()[0] ) );
        var addTask = Block(
            [temp],
            Assign( temp, counter ),
            //Invoke( LoggerExpression.Log( "addtask.temp" ), Convert( temp, typeof( object ) ) ),
            //Invoke( LoggerExpression.Log( "addtask.counter" ), Convert( counter, typeof( object ) ) ),
            Call( tasks, typeof( List<Task> ).GetMethod( nameof( List<Task>.Add ) )!, taskRun )
        );


        var counterInit = Assign( counter, Constant( 0 ) );

        var condition = LessThan( counter, Constant( 2 ) );  // TODO: partitionList.Count
        var iteration = PostIncrementAssign( counter );

        var forExpr = For( counterInit, condition, iteration, addTask );


        //var foreachPartition = ForEach( getPartitionsCall, partition, addTask );

        // await Task.WhenAll( tasks );
        var taskWhenAll = Await( Call(
            typeof( Task ).GetMethod( nameof( Task.WhenAll ), [typeof( IEnumerable<Task> )] ),
            tasks )
        );

        return BlockAsync(  // SM<10>  // temp, counter, partitionList, enumerator, tasks
            [tasks, counter, partitionList],
            initalizeTasks,
            Assign( partitionList, getPartitionsCall ),
            //Invoke( LoggerExpression.Log( "forEachAsync.partitionList" ), Convert( partitionList, typeof( object ) ) ),
            forExpr,  //foreachPartition,
            taskWhenAll
        );
    }
}

public static class LoggerExpression
{
    public static Expression<Action<object>> Log( string message )
    {
        return arg1 => Log( message, arg1 );
    }

    public static void Log( string message, object arg1 )
    {
        Console.WriteLine( $"{message} value: {arg1}" );
    }
}
