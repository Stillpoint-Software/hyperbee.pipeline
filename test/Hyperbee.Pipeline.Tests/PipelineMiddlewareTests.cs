using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineMiddlewareTests
{
    [TestMethod]
    public async Task Pipeline_should_call_hook_for_statement()
    {
        var command = PipelineFactory
            .Start<string>()
            .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{1}", result );
    }

    [TestMethod]
    public async Task Pipeline_should_call_hook_for_every_statement()
    {
        var command = PipelineFactory
            .Start<string>()
            .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{1}{2}", result );
    }

    [TestMethod]
    public async Task Pipeline_wrap_should_execute_in_correct_order()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .WrapAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
            .Pipe( ( ctx, arg ) => arg + "3" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{12}3", result );
    }

    [TestMethod]
    public async Task Pipeline_nested_hook_should_only_hook_the_inner_builder()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .Pipe( builder => builder
                .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
                .Pipe( ( ctx, arg ) => arg + "3" ) )
            .Pipe( ( ctx, arg ) => arg + "4" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "12{3}4", result );
    }

    [TestMethod]
    public async Task Pipeline_nested_wrap_should_only_wrap_the_inner_builder()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .Pipe( builder => builder
                .Pipe( ( ctx, arg ) => arg + "3" )
                .Pipe( ( ctx, arg ) => arg + "4" )
                .WrapAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
                .Pipe( ( ctx, arg ) => arg + "5" ) )
            .Pipe( ( ctx, arg ) => arg + "6" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "12{34}56", result );
    }
}
