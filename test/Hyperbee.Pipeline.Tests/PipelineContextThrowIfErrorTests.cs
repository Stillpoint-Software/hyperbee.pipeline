using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineContextThrowIfErrorTests
{
    [TestMethod]
    public void ThrowIfError_should_not_throw_when_no_exception()
    {
        // Arrange
        var context = new PipelineContext();

        // Act & Assert
        context.ThrowIfError(); // should not throw
    }

    [TestMethod]
    public void ThrowIfError_should_throw_when_exception_is_set()
    {
        // Arrange
        var context = new PipelineContext
        {
            Exception = new InvalidOperationException( "test error" )
        };

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>( () => context.ThrowIfError() );
    }

    [TestMethod]
    public void ThrowIfError_should_preserve_original_stack_trace()
    {
        // Arrange
        var context = new PipelineContext();

        try
        {
            ThrowFromNestedMethod();
        }
        catch ( Exception ex )
        {
            context.Exception = ex;
        }

        // Act
        try
        {
            context.ThrowIfError();
            Assert.Fail( "Expected exception was not thrown." );
        }
        catch ( InvalidOperationException ex )
        {
            // Assert
            Assert.Contains( nameof( ThrowFromNestedMethod ), ex.StackTrace! );
        }
    }

    private static void ThrowFromNestedMethod()
    {
        throw new InvalidOperationException( "original error" );
    }
}
