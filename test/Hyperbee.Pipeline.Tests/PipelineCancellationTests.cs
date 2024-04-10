using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineCancellationTests
{
    [TestMethod]
    public async Task Pipeline_cancellation_should_halt_processing()
    {
        var command = PipelineFactory
            .Start<int>()
            .Pipe( ( ctx, arg ) => 1 )
            .Pipe( ( ctx, arg ) =>
            {
                ctx.CancelAfter();
                return 2;
            } )
            .Pipe( ( ctx, arg ) => 3 )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public async Task Pipeline_cancellation_should_halt_processing_on_true_condition()
    {
        var command = PipelineFactory
            .Start<int>()
            .Pipe( ( ctx, arg ) => 1 )
            .CancelIf( ( ctx, arg ) => 1 + 1 == 2 )
            .Pipe( ( ctx, arg ) => 3 )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( 1, result );
    }

    [TestMethod]
    public async Task Pipeline_cancellation_extension_should_halt_processing()
    {
        var command = PipelineFactory
            .Start<int>()
            .Pipe( ( ctx, arg ) => 1 )
            .CancelWith( ( ctx, arg ) => 2 )
            .Pipe( ( ctx, arg ) => 3 )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( 2, result );
    }

    [TestMethod]
    public async Task Pipeline_cancellation_should_return_final_value_shape()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => "1" )
            .Pipe( ( ctx, arg ) =>
            {
                ctx.CancelAfter();
                return "2";
            } )
            .Pipe( ( ctx, arg ) => int.Parse( arg ) )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.IsInstanceOfType( result, typeof( int ) );
        Assert.AreEqual( 2, result );
    }

}
