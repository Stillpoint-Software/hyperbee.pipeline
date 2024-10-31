﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineExecutionTests
{
    [TestMethod]
    public async Task Pipeline_should_execute_function()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => int.Parse( arg ) )
            .Build();

        var result = await command( new PipelineContext(), "5" );

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public async Task Pipeline_should_execute_procedure()
    {
        var value = 0;
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) =>
            {
                value = int.Parse( arg );
                return value;
            } )
            .BuildAsProcedure();

        await command( new PipelineContext(), "5" );

        Assert.AreEqual( 5, value );
    }

    [TestMethod]
    public async Task Pipeline_should_execute_asynchronously()
    {
        var count = 0;

        var command = PipelineFactory
            .Start<int>()
            .WaitAll( builders => builders.Create(
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) ),
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) )
                ),
                reducer: ( ctx, arg, results ) => { return arg + results.Sum( x => (int) x.Result ); }
            )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( 2, count );
        Assert.AreEqual( 3, result );
    }

    [TestMethod]
    public async Task Pipeline_should_execute_asynchronously_with_default_reducer()
    {
        var count = 0;

        var command = PipelineFactory
            .Start<int>()
            .WaitAll( builders => builders.Create(
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) ),
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) )
                )
            )
            .Build();

        var result = await command( new PipelineContext(), 5 );

        Assert.AreEqual( 2, count );
        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public async Task Pipeline_should_execute_child_pipeline()
    {
        var command2 = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"{arg} again!" )
            .Build();

        var command1 = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeAsync( command2 )
            .Build();

        var result = await command1( new PipelineContext(), "pipeline" );

        Assert.AreEqual( "hello pipeline again!", result );
    }

    [TestMethod]
    public async Task Pipeline_should_do_the_big_kitchen_sink()
    {
        var count = 0;

        var command = PipelineFactory
            .Start<string>()
            .WithUser( "bob" )
            .WithLogging()
            .Pipe( ( ctx, arg ) => int.Parse( arg ), name: "a" )
            .Pipe( ( ctx, arg ) => arg + 100, name: "b" )
            .PipeAsync( ( ctx, arg ) => Task.FromResult( arg + 110 ), name: "c" )
            .Pipe( ( ctx, arg ) => arg + 120, name: "d" )
            .PipeIf( ( ctx, arg ) => 1 + 1 == 2, inheritMiddleware: false, builder => builder
                .WithUser( "jim" )
                .WithTiming()
                .Pipe( ( ctx, arg ) => arg * 2, name: "e.1" )
                .Pipe( ( ctx, arg ) => (arg / 2).ToString(), name: "e.2" )
                .Pipe( ( ctx, arg ) => int.Parse( arg ), name: "e.3" )
                .WithTransaction() )
            .Pipe( ( ctx, arg ) => arg + 130, name: "f" )
            .WaitAll( builders => builders.Create(
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ), name: "g.1" ),
                    builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ), name: "g.2" )
                ),
                reducer: ( ctx, arg, results ) => // reducer is optional
                {
                    var value = (long) arg;
                    return value + results.Sum( x => (int) x.Result ); // value should be 7
                }
            )
            .Pipe( ( ctx, arg ) =>
            {
                ctx.CancelAfter();
                return arg;
            }, name: "h" )
            .Pipe( ( ctx, arg ) => arg + 140, name: "i" )
            .Pipe( ( ctx, arg ) => (arg + 5).ToString(), name: "j" )
            .WithTransaction()
            .Build();

        var result = await command( new PipelineContext(), "2" );

        Assert.AreEqual( "465", result );
        Assert.AreEqual( 2, count );
    }
}


