using System;
using System.Diagnostics;

namespace Hyperbee.Pipeline.Tests.TestSupport;

public static class PipelineMiddleware
{
    public static IPipelineStartBuilder<TInput, TOutput> WithTiming<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> builder )
    {
        var timer = Stopwatch.StartNew();

        return builder.HookAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin timing (milliseconds = {timer.ElapsedMilliseconds}, ticks = {timer.ElapsedTicks})" );

            timer.Restart();
            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end timing (result = {result}) (milliseconds = {timer.ElapsedMilliseconds}, ticks = {timer.ElapsedTicks})" );

            return result;
        } );
    }

    public static IPipelineStartBuilder<TInput, TOutput> WithLogging<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> builder )
    {
        return builder.HookAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin (name = '{context.Name}') (arg = {argument})" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end (name = '{context.Name}') (result = {result})" );

            return result;
        } );
    }

    public static IPipelineStartBuilder<TInput, TOutput> WithUser<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> builder, string user )
    {
        return builder.HookAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin (user = '{user}') (arg = {argument})" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end (result = {result})" );

            return result;
        } );
    }

    public static IPipelineBuilder<TInput, TOutput> WithTransaction<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> builder )
    {
        return builder.WrapAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin transaction (name = '{context.Name}')" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end transaction (name = '{context.Name}')" );

            return result;
        }, "T" );
    }
}