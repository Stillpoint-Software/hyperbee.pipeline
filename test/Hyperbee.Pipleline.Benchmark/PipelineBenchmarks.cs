using System.ComponentModel.Design;
using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Hyperbee.Pipeline.Auth;
using Hyperbee.Pipeline.Caching;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Benchmark;

public class PipelineBenchmarks
{
    private FunctionAsync<string, int> _commandExecution;
    private FunctionAsync<string, string> _commandMiddleware;
    private FunctionAsync<string, string> _commandEnumeration;
    private FunctionAsync<int, int> _commandCancellation;
    private FunctionAsync<string, int> _commandAuth;

    [GlobalSetup]
    public void Setup()
    {
        _commandExecution = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => int.Parse( arg ) )
            .Build();

        _commandMiddleware = PipelineFactory
            .Start<string>()
            .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
            .Pipe( ( ctx, arg ) => arg + "1" )
            .Build();

        var count = 0;

        var command1 = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "!" )
            .Pipe( ( ctx, arg ) => count += 10 )
            .Build();

        _commandEnumeration = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
            .PipeAsync( async ( ctx, arg ) =>
            {
                foreach ( var e in arg )
                    await command1( ctx, e );

                return arg;
            } )
            .Pipe( ( ctx, arg ) => string.Join( ' ', arg ) )
            .Build();

        _commandCancellation = PipelineFactory
            .Start<int>()
            .Pipe( ( ctx, arg ) => 1 )
            .Pipe( ( ctx, arg ) =>
            {
                ctx.CancelAfter();
                return 2;
            } )
            .Pipe( ( ctx, arg ) => 3 )
            .Build();

        _commandAuth = PipelineFactory
            .Start<string>()
            .PipeIfClaim( new Claim( "Role", "reader" ), b => b.Pipe( Complex ) )
            .Build();

    }

    [Benchmark]
    public void PipelineExecution()
    {
        _commandExecution( new PipelineContext(), "5" );
    }

    [Benchmark]
    public void PipelineMiddleware()
    {
        _commandMiddleware( new PipelineContext() );
    }

    [Benchmark]
    public void PipelineEnumeration()
    {
        _commandEnumeration( new PipelineContext(), "e f" );
    }

    [Benchmark]
    public void PipelineCancellation()
    {
        _commandCancellation( new PipelineContext() );
    }

    [Benchmark]
    public void PipelineAuth()
    {
        var factory = CreateContextFactory();
        ILogger<PipelineBenchmarks> logger = null!;

        _commandAuth( factory.Create( logger ), "reader" );
    }

    [Benchmark]
    public void PipelineMemoryCache()
    {
        var command = PipelineFactory
            .Start<string>()
            .PipeCache( Complex )
            .Build();

        var factory = CreateContextFactory();
        ILogger<PipelineBenchmarks> logger = null!;

        command( factory.Create( logger ), "test" );
    }

    [Benchmark]
    public void PipelineDistributedCache()
    {
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( ComplexAsync )
            .Build();

        var factory = CreateContextFactory();
        ILogger<PipelineBenchmarks> logger = null!;

        command( factory.Create( logger ), "test" );
    }


    private static int Complex( IPipelineContext context, string argument ) => argument.Length;

    private static Task<int> ComplexAsync( IPipelineContext context, string argument ) =>
        Task.FromResult( argument.Length );

    private static IPipelineContextFactory CreateContextFactory( ISystemClock? clock = null )
    {
        clock ??= new TestSystemClock { UtcNow = DateTimeOffset.UtcNow };
        var container = new ServiceContainer();

        container.AddService(
            typeof( IMemoryCache ),
            new MemoryCache( new MemoryCacheOptions
            {
                Clock = clock,
                ExpirationScanFrequency = TimeSpan.FromMilliseconds( 100 ),
                TrackLinkedCacheEntries = false
            } ) );

        container.AddService(
            typeof( PipelineMemoryCacheOptions ),
            new PipelineMemoryCacheOptions()
        );

        return PipelineContextFactory.CreateFactory( container, true );
    }

    public class TestSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
