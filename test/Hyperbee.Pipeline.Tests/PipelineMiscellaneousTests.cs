using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineMiscellaneousTests
{
    [TestMethod]
    public async Task Pipeline_configure_should_be_seen_by_statement()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"not now {ctx.Name}", ctx => ctx.Name = "kittay!" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "not now kittay!", result );
    }

    [TestMethod]
    public async Task Pipeline_should_honor_if_condition()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .PipeIf( ( ctx, arg ) => false, builder => builder
                .Pipe( ( ctx, arg ) => arg + "3" ) )
            .Pipe( ( ctx, arg ) => arg + "4" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "124", result );
    }
}
