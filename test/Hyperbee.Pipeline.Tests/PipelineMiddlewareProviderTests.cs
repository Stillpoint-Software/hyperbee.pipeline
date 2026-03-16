using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineMiddlewareProviderTests
{
    private class TestMiddlewareProvider : IPipelineMiddlewareProvider
    {
        private readonly List<MiddlewareAsync<object, object>> _hooks = [];
        private readonly List<MiddlewareAsync<object, object>> _wraps = [];

        public TestMiddlewareProvider AddHook( MiddlewareAsync<object, object> hook )
        {
            _hooks.Add( hook );
            return this;
        }

        public TestMiddlewareProvider AddWrap( MiddlewareAsync<object, object> wrap )
        {
            _wraps.Add( wrap );
            return this;
        }

        public IEnumerable<MiddlewareAsync<object, object>> Hooks => _hooks;
        public IEnumerable<MiddlewareAsync<object, object>> Wraps => _wraps;
    }

    // UseHooks / UseWraps tests

    [TestMethod]
    public async Task UseHooks_should_apply_hooks_from_provider()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" );

        var command = PipelineFactory
            .Start<string>()
            .UseHooks( provider )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{1}", result );
    }

    [TestMethod]
    public async Task UseHooks_should_apply_multiple_hooks_from_provider()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "[" ) + "]" );

        var command = PipelineFactory
            .Start<string>()
            .UseHooks( provider )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var result = await command( new PipelineContext() );

        // Each hook wraps independently per the HookAsync(IEnumerable) implementation
        Assert.AreEqual( "[1]", result );
    }

    [TestMethod]
    public async Task UseWraps_should_apply_wraps_from_provider()
    {
        var provider = new TestMiddlewareProvider()
            .AddWrap( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" );

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .UseWraps( provider )
            .Pipe( ( ctx, arg ) => arg + "3" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{12}3", result );
    }

    [TestMethod]
    public async Task UseHooks_and_UseWraps_should_work_together()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "(" ) + ")" )
            .AddWrap( async ( ctx, arg, next ) => await next( ctx, arg + "[" ) + "]" );

        var command = PipelineFactory
            .Start<string>()
            .UseHooks( provider )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Pipe( ( ctx, arg ) => arg + "2" )
            .UseWraps( provider )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "[(1)(2)]", result );
    }

    [TestMethod]
    public async Task UseHooks_with_null_provider_should_be_no_op()
    {
        var command = PipelineFactory
            .Start<string>()
            .UseHooks( null )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "1", result );
    }

    [TestMethod]
    public async Task UseWraps_with_null_provider_should_be_no_op()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .UseWraps( null )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "1", result );
    }

    [TestMethod]
    public async Task UseHooks_with_empty_provider_should_be_no_op()
    {
        var provider = new TestMiddlewareProvider();

        var command = PipelineFactory
            .Start<string>()
            .UseHooks( provider )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "1", result );
    }

    [TestMethod]
    public async Task UseWraps_with_empty_provider_should_be_no_op()
    {
        var provider = new TestMiddlewareProvider();

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "1" )
            .UseWraps( provider )
            .Build();

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "1", result );
    }

    // PipelineFactory.Create tests

    [TestMethod]
    public async Task Create_should_apply_hooks_and_wraps_from_provider()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "(" ) + ")" )
            .AddWrap( async ( ctx, arg, next ) => await next( ctx, arg + "[" ) + "]" );

        var command = PipelineFactory.Create<string, string>( provider, builder =>
            builder
                .Pipe( ( ctx, arg ) => arg + "1" )
                .Pipe( ( ctx, arg ) => arg + "2" )
        );

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "[(1)(2)]", result );
    }

    [TestMethod]
    public async Task Create_should_work_with_hooks_only()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" );

        var command = PipelineFactory.Create<string, string>( provider, builder =>
            builder
                .Pipe( ( ctx, arg ) => arg + "1" )
        );

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "{1}", result );
    }

    [TestMethod]
    public async Task Create_should_work_with_wraps_only()
    {
        var provider = new TestMiddlewareProvider()
            .AddWrap( async ( ctx, arg, next ) => await next( ctx, arg + "[" ) + "]" );

        var command = PipelineFactory.Create<string, string>( provider, builder =>
            builder
                .Pipe( ( ctx, arg ) => arg + "1" )
                .Pipe( ( ctx, arg ) => arg + "2" )
        );

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "[12]", result );
    }

    [TestMethod]
    public void Create_should_throw_with_null_provider()
    {
        Assert.ThrowsExactly<System.ArgumentNullException>( () =>
            PipelineFactory.Create<string, string>( null, builder =>
                builder
                    .Pipe( ( ctx, arg ) => arg + "1" )
            )
        );
    }

    [TestMethod]
    public async Task Create_should_work_with_empty_provider()
    {
        var provider = new TestMiddlewareProvider();

        var command = PipelineFactory.Create<string, string>( provider, builder =>
            builder
                .Pipe( ( ctx, arg ) => arg + "1" )
        );

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "1", result );
    }

    [TestMethod]
    public async Task Create_should_work_with_async_steps()
    {
        var provider = new TestMiddlewareProvider()
            .AddHook( async ( ctx, arg, next ) => await next( ctx, arg + "(" ) + ")" );

        var command = PipelineFactory.Create<string, string>( provider, builder =>
            builder
                .PipeAsync( async ( ctx, arg ) =>
                {
                    await Task.Delay( 1 );
                    return arg + "async";
                } )
        );

        var result = await command( new PipelineContext() );

        Assert.AreEqual( "(async)", result );
    }
}
