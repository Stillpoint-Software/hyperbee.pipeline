using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Middleware;

namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class ExceptionHandlingTests
{
    private static string ThrowInvalidOperation( IPipelineContext ctx, string arg )
    {
        throw new InvalidOperationException( "Test error" );
    }

    private static string ThrowArgument( IPipelineContext ctx, string arg )
    {
        throw new ArgumentException( "Unmapped" );
    }

    private static string ThrowDerived( IPipelineContext ctx, string arg )
    {
        throw new InvalidOperationException( "Derived" );
    }

    [TestMethod]
    public async Task Pipeline_should_handle_mapped_exception()
    {
        var command = PipelineFactory
            .Start<string>()
            .WithExceptionHandling( config => config
                .AddException<InvalidOperationException>( errorcode: 1001 )
            )
            .Pipe( ThrowInvalidOperation )
            .Build();

        var context = new PipelineContext();
        await command( context, "input" );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );

        var failures = context.ValidationFailures().ToList();
        Assert.AreEqual( 1, failures.Count );
        Assert.AreEqual( "1001", failures[0].ErrorCode );
        Assert.IsTrue( failures[0].ErrorMessage.Contains( "Test error" ) );
    }

    [TestMethod]
    public async Task Pipeline_should_rethrow_unmapped_exception()
    {
        var command = PipelineFactory
            .Start<string>()
            .WithExceptionHandling( config => config
                .AddException<InvalidOperationException>( errorcode: 1001 )
            )
            .Pipe( ThrowArgument )
            .Build();

        var context = new PipelineContext();
        await command( context, "input" );

        // Unmapped exceptions are re-thrown by the middleware, then caught
        // by the pipeline's outer handler which stores them on context.Exception
        Assert.IsNotNull( context.Exception );
        Assert.IsInstanceOfType( context.Exception, typeof( ArgumentException ) );
        Assert.IsTrue( context.IsValid() ); // No validation failure was set
    }

    [TestMethod]
    public async Task Pipeline_should_not_set_error_when_no_exception()
    {
        var command = PipelineFactory
            .Start<string>()
            .WithExceptionHandling( config => config
                .AddException<InvalidOperationException>( errorcode: 1001 )
            )
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .Build();

        var context = new PipelineContext();
        var result = await command( context, "world" );

        Assert.IsTrue( context.IsValid() );
        Assert.AreEqual( "hello world", result );
    }

    [TestMethod]
    public async Task Pipeline_should_handle_derived_exception_type()
    {
        var command = PipelineFactory
            .Start<string>()
            .WithExceptionHandling( config => config
                .AddException<Exception>( errorcode: 9999 )
            )
            .Pipe( ThrowDerived )
            .Build();

        var context = new PipelineContext();
        await command( context, "input" );

        Assert.IsFalse( context.IsValid() );
        var failures = context.ValidationFailures().ToList();
        Assert.AreEqual( "9999", failures[0].ErrorCode );
    }
}
