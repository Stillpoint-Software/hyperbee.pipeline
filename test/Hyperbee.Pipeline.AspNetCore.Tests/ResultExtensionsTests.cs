using Hyperbee.Pipeline.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.AspNetCore.Tests;

[TestClass]
public class ResultExtensionsTests
{
    // WithHeader tests

    [TestMethod]
    public async Task WithHeader_should_set_response_header()
    {
        var inner = Results.Ok( "hello" );
        var result = inner.WithHeader( "ETag", "\"abc123\"" );

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
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
    public async Task WithHeader_should_chain_multiple_headers()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "ETag", "\"abc\"" )
            .WithHeader( "X-Custom", "test" );

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "\"abc\"", httpContext.Response.Headers["ETag"].ToString() );
        Assert.AreEqual( "test", httpContext.Response.Headers["X-Custom"].ToString() );
    }

    // WithNoContent tests

    [TestMethod]
    public void WithNoContent_should_return_no_content_result()
    {
        var inner = Results.Ok( "hello" );
        var result = inner.WithNoContent();

        Assert.IsInstanceOfType( result, typeof( NoContent ) );
    }

    [TestMethod]
    public async Task WithNoContent_should_preserve_headers_from_prior_decorator()
    {
        var result = Results.Ok( "hello" )
            .WithHeader( "X-Trace", "abc123" )
            .WithNoContent();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        await result.ExecuteAsync( httpContext );

        Assert.AreEqual( "abc123", httpContext.Response.Headers["X-Trace"].ToString() );
        Assert.AreEqual( StatusCodes.Status204NoContent, httpContext.Response.StatusCode );
    }
}
