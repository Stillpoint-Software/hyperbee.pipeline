using System.Collections.Concurrent;
using System.ComponentModel.Design;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hyperbee.Pipeline.Caching.Tests;

[TestClass]
public class PipelineDistributedCachingTests
{

    [TestMethod]
    public async Task Should_ReturnDifferentResults_WhenUsingDefaultKeys()
    {
        // Arrange
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( ComplexAsync )
            .Build();

        var factory = CreateContextFactory();
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), "test" );
        var result2 = await command( factory.Create( logger ), "test" );
        var result3 = await command( factory.Create( logger ), "test more" );

        // Assert
        Assert.AreEqual( 4, result1 );
        Assert.AreEqual( 4, result2 );
        Assert.AreEqual( 9, result3 );
    }

    [TestMethod]
    public async Task Should_ReturnDifferentResults_WhenUsingDefaultKeys_WithNestedAsyncPipeline()
    {
        // Arrange
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( b => b
                .PipeAsync( ComplexAsync )
                .Pipe( ( ctx, arg ) => arg + 100 ) )
            .Build();

        var factory = CreateContextFactory();
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), "test async" );
        var result2 = await command( factory.Create( logger ), "test async" );
        var result3 = await command( factory.Create( logger ), "test async more" );

        // Assert
        Assert.AreEqual( 110, result1 );
        Assert.AreEqual( 110, result2 );
        Assert.AreEqual( 115, result3 );
    }

    [TestMethod]
    public async Task Should_ReturnDifferentResults_WhenUsingDefaultKeys_WithNestedPipeline()
    {
        // Arrange
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( b => b
                .Pipe( Complex )
                .Pipe( ( ctx, arg ) => arg + 100 ) )
            .Build();

        var factory = CreateContextFactory();
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), "test" );
        var result2 = await command( factory.Create( logger ), "test" );
        var result3 = await command( factory.Create( logger ), "test more" );

        // Assert
        Assert.AreEqual( 104, result1 );
        Assert.AreEqual( 104, result2 );
        Assert.AreEqual( 109, result3 );
    }

    [TestMethod]
    public async Task Should_ReturnDifferentResults_WhenUsingCustomKeys()
    {
        // Arrange
        var command = PipelineFactory
            .Start<(string Tenant, string Value)>()
            .PipeDistributedCacheAsync( ComplexAsync,
                ( input, options ) =>
                {
                    options.Key = $"custom/{input.Tenant}/{input.Value}";
                    options.AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromMilliseconds( 200 );
                    return options;
                } )
            .Build();

        var factory = CreateContextFactory();
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), ("CompanyA", "test company") );
        var result2 = await command( factory.Create( logger ), ("OrganizationB", "test organization") );

        // Assert
        Assert.AreEqual( 12, result1 );
        Assert.AreEqual( 17, result2 );
    }

    [TestMethod]
    public async Task Should_ReturnDifferentResults_WhenCacheExpires()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var expireTime = startTime + TimeSpan.FromMinutes( 10 );

        var clock = new TestSystemClock { UtcNow = startTime };
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( ComplexAsync,
                ( input, options ) =>
                {
                    options.Key = "TESTING_EXPIRING";
                    options.SetAbsoluteExpiration( expireTime );
                    return options;
                } )
            .Build();


        var factory = CreateContextFactory( clock );
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), "testing" );
        var result2 = await command( factory.Create( logger ), "testing more" );

        clock.UtcNow += TimeSpan.FromMinutes( 20 ); // Fast-forward time by 20 minutes

        var result3 = await command( factory.Create( logger ), "testing more again" );

        // Assert
        Assert.AreEqual( 7, result1 );
        Assert.AreEqual( 7, result2 );
        Assert.AreEqual( 18, result3 );
    }

    [TestMethod]
    public async Task Should_ReturnSameResults_WhenUsingSameKeys()
    {
        // Arrange
        var command = PipelineFactory
            .Start<string>()
            .PipeDistributedCacheAsync( ComplexAsync,
                ( _, options ) =>
                {
                    options.Key = "USING_SAME_KEY";
                    return options;
                } )
            .Build();

        var factory = CreateContextFactory();
        var logger = Substitute.For<ILogger>();

        // Act
        var result1 = await command( factory.Create( logger ), "a" );
        var result2 = await command( factory.Create( logger ), "bc" );

        // Assert
        Assert.AreEqual( 1, result1 );
        Assert.AreEqual( 1, result2 ); // same as previous
    }

    private static Task<int> ComplexAsync( IPipelineContext context, string argument ) =>
        Task.FromResult( argument.Length );

    private static Task<int> ComplexAsync( IPipelineContext context, (string Tenant, string Value) argument ) =>
        Task.FromResult( argument.Value.Length );

    private static int Complex( IPipelineContext context, string argument ) => argument.Length;

    private static IPipelineContextFactory CreateContextFactory( ISystemClock? clock = null )
    {
        clock ??= new TestSystemClock { UtcNow = DateTimeOffset.UtcNow };
        var container = new ServiceContainer();

        var cache = new MemoryDistributedCacheOptions
        {
            Clock = clock,
            ExpirationScanFrequency = TimeSpan.FromMilliseconds( 100 )
        };

        var options = Substitute.For<IOptions<MemoryDistributedCacheOptions>>();
        options.Value.Returns( cache );

        container.AddService(
            typeof( IDistributedCache ),
            new MemoryDistributedCache( options ) );

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
//
//
// public class TestDistributedCache( MemoryCacheOptions options ) : IDistributedCache
// {
//     private readonly MemoryCache _cache = new( options );
//
//     public byte[]? Get( string key )
//     {
//         return _cache.Get(key) as byte[];
//     }
//
//     public Task<byte[]?> GetAsync( string key, CancellationToken token = default )
//     {
//         return Task.FromResult( this.Get( key ) );
//     }
//
//     public void Set( string key, byte[] value, DistributedCacheEntryOptions options )
//     {
//         _cache.Set( key, value,
//             new MemoryCacheEntryOptions
//             {
//                 AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
//                 AbsoluteExpiration = options.AbsoluteExpiration,
//                 SlidingExpiration = options.SlidingExpiration,
//             } );
//     }
//
//     public Task SetAsync( string key, byte[] value, DistributedCacheEntryOptions options,
//         CancellationToken token = default )
//     {
//         this.Set( key, value, options );
//         return Task.CompletedTask;
//     }
//
//     public void Refresh( string key )
//     {
//         // NoOp
//     }
//
//     public Task RefreshAsync( string key, CancellationToken token = default )
//     {
//         // NoOp
//         return Task.CompletedTask;
//     }
//
//     public void Remove( string key )
//     {
//         _cache.Remove( key );
//     }
//
//     public Task RemoveAsync( string key, CancellationToken token = default )
//     {
//         this.Remove( key );
//         return Task.CompletedTask;
//     }
// }
