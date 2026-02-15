using FluentValidation.Results;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class ValidationContextTests
{
    [TestMethod]
    public void Context_should_be_valid_with_no_validation_result()
    {
        var context = new PipelineContext();

        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public void Context_should_be_valid_with_valid_result()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationResult() );

        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public void Context_should_not_be_valid_with_invalid_result()
    {
        var context = new PipelineContext();
        var failure = new ValidationFailure( "Name", "Name is required." );
        context.SetValidationResult( failure );

        Assert.IsFalse( context.IsValid() );
    }

    [TestMethod]
    public void SetValidationResult_should_store_result()
    {
        var context = new PipelineContext();
        var result = new ValidationResult( new[] { new ValidationFailure( "Age", "Invalid age." ) } );

        context.SetValidationResult( result );

        var stored = context.GetValidationResult();
        Assert.IsNotNull( stored );
        Assert.HasCount( 1, stored.Errors );
        Assert.AreEqual( "Age", stored.Errors[0].PropertyName );
    }

    [TestMethod]
    public void SetValidationResult_with_failure_should_store_in_context()
    {
        var context = new PipelineContext();
        var failure = new ValidationFailure( "Email", "Email is invalid." );

        context.SetValidationResult( failure );

        var stored = context.GetValidationResult();
        Assert.IsNotNull( stored );
        Assert.HasCount( 1, stored.Errors );
        Assert.AreEqual( "Email", stored.Errors[0].PropertyName );
    }

    [TestMethod]
    public void SetValidationResult_with_failure_list_should_store_all_failures()
    {
        var context = new PipelineContext();
        var failures = new List<ValidationFailure>
        {
            new( "Name", "Name is required." ),
            new( "Age", "Age must be positive." )
        };

        context.SetValidationResult( (IReadOnlyList<ValidationFailure>) failures );

        var stored = context.GetValidationResult();
        Assert.IsNotNull( stored );
        Assert.HasCount( 2, stored.Errors );
    }

    [TestMethod]
    public void SetValidationResult_with_cancel_after_should_cancel_pipeline()
    {
        var context = new PipelineContext();
        var failure = new ValidationFailure( "Name", "Required." );

        context.SetValidationResult( failure, ValidationAction.CancelAfter );

        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public void SetValidationResult_with_continue_after_should_not_cancel()
    {
        var context = new PipelineContext();
        var failure = new ValidationFailure( "Name", "Required." );

        context.SetValidationResult( failure, ValidationAction.ContinueAfter );

        Assert.IsFalse( context.IsCanceled );
    }

    [TestMethod]
    public void AddValidationResult_should_append_to_existing()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationFailure( "Name", "First error." ) );

        context.AddValidationResult( new ValidationFailure( "Age", "Second error." ) );

        var stored = context.GetValidationResult();
        Assert.IsNotNull( stored );
        Assert.HasCount( 2, stored.Errors );
        Assert.AreEqual( "Name", stored.Errors[0].PropertyName );
        Assert.AreEqual( "Age", stored.Errors[1].PropertyName );
    }

    [TestMethod]
    public void AddValidationResult_with_no_existing_should_create_new()
    {
        var context = new PipelineContext();

        context.AddValidationResult( new ValidationFailure( "Name", "Error." ) );

        var stored = context.GetValidationResult();
        Assert.IsNotNull( stored );
        Assert.HasCount( 1, stored.Errors );
    }

    [TestMethod]
    public void ClearValidationResult_should_remove_result()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationFailure( "Name", "Error." ) );

        context.ClearValidationResult();

        Assert.IsNull( context.GetValidationResult() );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public void ValidationFailures_with_no_result_should_return_empty()
    {
        var context = new PipelineContext();

        var failures = context.ValidationFailures();

        Assert.IsFalse( failures.Any() );
    }

    [TestMethod]
    public void ValidationFailures_with_result_should_return_all_failures()
    {
        var context = new PipelineContext();
        context.SetValidationResult( new ValidationFailure( "A", "Error A." ) );
        context.AddValidationResult( new ValidationFailure( "B", "Error B." ) );

        var failures = context.ValidationFailures().ToList();

        Assert.HasCount( 2, failures );
        Assert.AreEqual( "A", failures[0].PropertyName );
        Assert.AreEqual( "B", failures[1].PropertyName );
    }

    [TestMethod]
    public void FailAfter_should_set_result_and_cancel()
    {
        var context = new PipelineContext();

        context.FailAfter( "Something went wrong.", propertyName: "Operation" );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );

        var failures = context.ValidationFailures().ToList();
        Assert.HasCount( 1, failures );
        Assert.AreEqual( "Operation", failures[0].PropertyName );
        Assert.AreEqual( "Something went wrong.", failures[0].ErrorMessage );
    }

    [TestMethod]
    public void FailAfter_with_code_should_set_error_code()
    {
        var context = new PipelineContext();

        context.FailAfter( "Not found.", 404, propertyName: "Item" );

        var failures = context.ValidationFailures().ToList();
        Assert.HasCount( 1, failures );
        Assert.AreEqual( "404", failures[0].ErrorCode );
        Assert.AreEqual( "Item", failures[0].PropertyName );
    }

    [TestMethod]
    public void GetValidationResult_should_return_null_when_not_set()
    {
        var context = new PipelineContext();

        Assert.IsNull( context.GetValidationResult() );
    }
}
