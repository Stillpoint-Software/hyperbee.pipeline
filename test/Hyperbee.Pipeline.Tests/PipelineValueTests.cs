using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineValueTests
{
    [TestMethod]
    public async Task Pipeline_should_mutate_value()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .Build();

        var result = await command( new PipelineContext(), "pipeline" );

        Assert.AreEqual( "hello pipeline", result );
    }

    [TestMethod]
    public async Task Pipeline_should_mutate_value_shape()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => int.Parse( arg ) )
            .Build();

        var result = await command( new PipelineContext(), "5" );

        Assert.AreEqual( 5, result );
    }

    [TestMethod]
    public async Task Pipeline_call_block_should_not_mutate_trailing_input()
    {
        var callResult = string.Empty;

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .Call( builder => builder
                .Call( ( ctx, arg ) => callResult = arg + "3" )
                .Pipe( ( ctx, arg ) => arg + "9" )
            )
            .Pipe( ( ctx, arg ) => arg + "4" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "124", result );
        Assert.AreEqual( "123", callResult );
    }

    [TestMethod]
    public async Task Pipeline_call_should_not_mutate_trailing_input()
    {
        var callResult = string.Empty;

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .Call( ( ctx, arg ) => callResult = arg + "3" )
            .Pipe( ( ctx, arg ) => arg + "4" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "124", result );
        Assert.AreEqual( "123", callResult );
    }
}
