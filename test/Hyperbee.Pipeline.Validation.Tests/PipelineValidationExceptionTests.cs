using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class PipelineValidationExceptionTests
{
    [TestMethod]
    public void Constructor_should_throw_for_null_validation_result()
    {
        Assert.ThrowsExactly<ArgumentNullException>( () => _ = new PipelineValidationException( null! ) );
    }

    [TestMethod]
    public void Constructor_with_message_should_throw_for_null_validation_result()
    {
        Assert.ThrowsExactly<ArgumentNullException>( () => _ = new PipelineValidationException( "message", null! ) );
    }

    [TestMethod]
    public void Default_message_should_join_failure_messages()
    {
        var result = new ValidationResult( new[]
        {
            new ValidationFailure( "Name", "Name is required." ),
            new ValidationFailure( "Age", "Age must be positive." )
        } );

        var exception = new PipelineValidationException( result );

        Assert.AreEqual( "Validation failed: Name is required.; Age must be positive.", exception.Message );
    }

    [TestMethod]
    public void Custom_message_should_be_preserved()
    {
        var result = new ValidationResult( new[] { new ValidationFailure( "Name", "Required." ) } );

        var exception = new PipelineValidationException( "custom message", result );

        Assert.AreEqual( "custom message", exception.Message );
        Assert.AreSame( result, exception.ValidationResult );
    }
}
