using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class ThrowIfInvalidTests
{
    [TestMethod]
    public void ThrowIfInvalid_should_not_throw_with_no_validation_result()
    {
        var context = new PipelineContext();

        context.ThrowIfInvalid();
    }

    [TestMethod]
    public void ThrowIfInvalid_should_not_throw_with_valid_result()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationResult() );

        context.ThrowIfInvalid();
    }

    [TestMethod]
    public void ThrowIfInvalid_should_throw_with_invalid_result()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationFailure( "Name", "Name is required." ) );

        var exception = Assert.ThrowsExactly<PipelineValidationException>( () => context.ThrowIfInvalid() );

        Assert.AreSame( context.GetValidationResult(), exception.ValidationResult );
        StringAssert.Contains( exception.Message, "Name is required." );
    }

    [TestMethod]
    public void ThrowIfInvalid_message_should_join_all_failure_messages()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationFailure( "Name", "Name is required." ) );
        context.AddValidationResult( new ValidationFailure( "Age", "Age must be positive." ) );

        var exception = Assert.ThrowsExactly<PipelineValidationException>( () => context.ThrowIfInvalid() );

        StringAssert.Contains( exception.Message, "Name is required." );
        StringAssert.Contains( exception.Message, "Age must be positive." );
    }

    [TestMethod]
    public void ThrowIfInvalid_should_throw_after_FailAfter()
    {
        var context = new PipelineContext();
        context.FailAfter( "Something went wrong.", propertyName: "Operation" );

        var exception = Assert.ThrowsExactly<PipelineValidationException>( () => context.ThrowIfInvalid() );

        Assert.HasCount( 1, exception.ValidationResult.Errors );
    }

    [TestMethod]
    public void ThrowIfInvalid_should_not_throw_when_canceled_without_validation_result()
    {
        // Cancellation is control flow, not a validation failure
        var context = new PipelineContext();
        context.CancelAfter();

        context.ThrowIfInvalid();
    }

    [TestMethod]
    public void ThrowIfInvalid_should_not_throw_when_error_without_validation_result()
    {
        // Exceptions are surfaced by ThrowIfError; the validation axis is independent
        var context = new PipelineContext { Exception = new InvalidOperationException( "boom" ) };

        context.ThrowIfInvalid();
    }

    [TestMethod]
    public async Task ThrowIfInvalid_inside_pipeline_step_should_be_captured_as_context_exception()
    {
        // The nested-command shape: a pipeline step surfaces an inner command's validation
        // failure with ThrowIfInvalid; Build() captures the throw on the outer context
        var innerContext = new PipelineContext();
        innerContext.SetValidationResult( new ValidationFailure( "Name", "Name is required." ) );

        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( _, arg ) =>
            {
                innerContext.ThrowIfInvalid();
                return arg;
            } )
            .Pipe( ( _, arg ) =>
            {
                followUpRan = true;
                return arg;
            } )
            .Build();

        var context = new PipelineContext();
        var result = await pipeline( context, "input" );

        Assert.IsNull( result );
        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsError );
        Assert.IsInstanceOfType( context.Exception, typeof( PipelineValidationException ) );
        Assert.IsTrue( context.IsCanceled );   // halt-on-error
        Assert.IsTrue( context.IsValid() );    // outer context carries no validation item
    }
}
