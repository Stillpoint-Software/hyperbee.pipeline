using Hyperbee.Pipeline.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.AspNetCore.Tests;

[TestClass]
public class ResultExtensionsTests
{
    private static HttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
    }

    // WithHeader tests

    [TestMethod]
    public async Task WithHeader_should_set_response_header()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"abc123\"" );

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"abc123\"", httpContext.Response.Headers["ETag"].ToString() );
    }

    [TestMethod]
    public void WithHeader_should_return_original_result_for_null_value()
    {
        var inner = Results.Ok( "hello" );
        var result = inner.WithHeader( "ETag", null );

        Assert.AreSame( inner, result );
    }

    [TestMethod]
    public void WithHeader_should_return_original_result_for_empty_value()
    {
        var inner = Results.Ok( "hello" );
        var result = inner.WithHeader( "ETag", "" );

        Assert.AreSame( inner, result );
    }

    [TestMethod]
    public void WithHeader_should_return_original_result_for_whitespace_value()
    {
        var inner = Results.Ok( "hello" );
        var result = inner.WithHeader( "ETag", "   " );

        Assert.AreSame( inner, result );
    }

    [TestMethod]
    public async Task WithHeader_should_chain_two_headers()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"abc\"" )
            .WithHeader( "X-Custom", "test" );

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"abc\"", httpContext.Response.Headers["ETag"].ToString() );
        Assert.AreEqual( "test", httpContext.Response.Headers["X-Custom"].ToString() );
    }

    [TestMethod]
    public async Task WithHeader_should_chain_three_headers()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"v1\"" )
            .WithHeader( "X-Request-Id", "req-123" )
            .WithHeader( "Cache-Control", "no-cache" );

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"v1\"", httpContext.Response.Headers["ETag"].ToString() );
        Assert.AreEqual( "req-123", httpContext.Response.Headers["X-Request-Id"].ToString() );
        Assert.AreEqual( "no-cache", httpContext.Response.Headers["Cache-Control"].ToString() );
    }

    [TestMethod]
    public async Task WithHeader_should_skip_null_values_in_chain()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"abc\"" )
            .WithHeader( "X-Skip", null )
            .WithHeader( "X-Custom", "kept" );

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"abc\"", httpContext.Response.Headers["ETag"].ToString() );
        Assert.AreEqual( "", httpContext.Response.Headers["X-Skip"].ToString() );
        Assert.AreEqual( "kept", httpContext.Response.Headers["X-Custom"].ToString() );
    }

    // WithNoContent tests

    [TestMethod]
    public void WithNoContent_should_return_no_content_result()
    {
        var result = Results.Ok( "hello" ).WithNoContent();

        Assert.IsInstanceOfType( result, typeof( NoContent ) );
    }

    [TestMethod]
    public async Task WithNoContent_should_preserve_single_header()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "X-Trace", "abc123" )
            .WithNoContent();

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "abc123", httpContext.Response.Headers["X-Trace"].ToString() );
        Assert.AreEqual( StatusCodes.Status204NoContent, httpContext.Response.StatusCode );
    }

    [TestMethod]
    public async Task WithNoContent_should_preserve_all_chained_headers()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"v1\"" )
            .WithHeader( "X-Request-Id", "req-456" )
            .WithHeader( "X-Correlation", "corr-789" )
            .WithNoContent();

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"v1\"", httpContext.Response.Headers["ETag"].ToString() );
        Assert.AreEqual( "req-456", httpContext.Response.Headers["X-Request-Id"].ToString() );
        Assert.AreEqual( "corr-789", httpContext.Response.Headers["X-Correlation"].ToString() );
        Assert.AreEqual( StatusCodes.Status204NoContent, httpContext.Response.StatusCode );
    }

    [TestMethod]
    public async Task WithNoContent_on_plain_result_should_return_204()
    {
        var result = Results.Ok( "hello" ).WithNoContent();

        var httpContext = CreateHttpContext();
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( StatusCodes.Status204NoContent, httpContext.Response.StatusCode );
    }
}
