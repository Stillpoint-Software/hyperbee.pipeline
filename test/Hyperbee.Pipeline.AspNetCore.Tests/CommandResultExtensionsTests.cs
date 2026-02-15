using FluentValidation.Results;
using Hyperbee.Pipeline.AspNetCore.Extensions;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hyperbee.Pipeline.AspNetCore.Tests;

[TestClass]
public class CommandResultExtensionsTests
{
    private static CommandResult<T> CreateSuccessResult<T>( T value )
    {
        var context = new PipelineContext();
        return new CommandResult<T>
        {
            Context = context,
            Result = value,
            CommandType = typeof( CommandResultExtensionsTests )
        };
    }

    private static CommandResult<T> CreateFailureResult<T>( params ValidationFailure[] failures )
    {
        var context = new PipelineContext();
        context.SetValidationResult(
            (IReadOnlyList<ValidationFailure>) failures.ToList(),
            ValidationAction.CancelAfter
        );
        return new CommandResult<T>
        {
            Context = context,
            CommandType = typeof( CommandResultExtensionsTests )
        };
    }

    private static CommandResult CreateNonGenericSuccessResult()
    {
        var context = new PipelineContext();
        return new CommandResult
        {
            Context = context,
            CommandType = typeof( CommandResultExtensionsTests )
        };
    }

    private static CommandResult CreateNonGenericFailureResult( params ValidationFailure[] failures )
    {
        var context = new PipelineContext();
        context.SetValidationResult(
            (IReadOnlyList<ValidationFailure>) failures.ToList(),
            ValidationAction.CancelAfter
        );
        return new CommandResult
        {
            Context = context,
            CommandType = typeof( CommandResultExtensionsTests )
        };
    }

    // ToResult<T> tests

    [TestMethod]
    public void ToResult_should_return_ok_for_valid_result()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void ToResult_should_return_404_for_not_found_failure()
    {
        var commandResult = CreateFailureResult<string>(
            new NotFoundValidationFailure( "Item", "Item not found." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status404NotFound, problem.StatusCode );
    }

    [TestMethod]
    public void ToResult_should_return_403_for_forbidden_failure()
    {
        var commandResult = CreateFailureResult<string>(
            new ForbiddenValidationFailure( "Resource", "Access denied." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status403Forbidden, problem.StatusCode );
    }

    [TestMethod]
    public void ToResult_should_return_401_for_unauthorized_failure()
    {
        var commandResult = CreateFailureResult<string>(
            new UnauthorizedValidationFailure( "User", "Not authenticated." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status401Unauthorized, problem.StatusCode );
    }

    [TestMethod]
    public void ToResult_should_return_400_for_application_failure()
    {
        var commandResult = CreateFailureResult<string>(
            new ApplicationValidationFailure( "Field", "Invalid." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status400BadRequest, problem.StatusCode );
    }

    [TestMethod]
    public void ToResult_should_return_400_for_general_validation_failure()
    {
        var commandResult = CreateFailureResult<string>(
            new ValidationFailure( "Name", "Name is required." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status400BadRequest, problem.StatusCode );
    }

    // ToResult with selector tests

    [TestMethod]
    public void ToResult_with_selector_should_transform_result()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult( s => new { Value = s.ToUpper() } );

        Assert.IsTrue( result.GetType().IsGenericType );
        Assert.AreEqual( "Ok`1", result.GetType().GetGenericTypeDefinition().Name );
    }

    [TestMethod]
    public void ToResult_with_selector_should_return_not_found_for_null()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult<string, string>( _ => null );

        Assert.IsInstanceOfType( result, typeof( NotFound ) );
    }

    [TestMethod]
    public void ToResult_with_selector_should_return_error_for_invalid()
    {
        var commandResult = CreateFailureResult<string>(
            new NotFoundValidationFailure( "Item", "Not found." )
        );

        var result = commandResult.ToResult( s => new { Value = s } );

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status404NotFound, problem.StatusCode );
    }

    // ToResult non-generic tests

    [TestMethod]
    public void ToResult_non_generic_should_return_ok()
    {
        var commandResult = CreateNonGenericSuccessResult();

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }

    [TestMethod]
    public void ToResult_non_generic_should_return_404_for_not_found()
    {
        var commandResult = CreateNonGenericFailureResult(
            new NotFoundValidationFailure( "Item", "Not found." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status404NotFound, problem.StatusCode );
    }

    [TestMethod]
    public void ToResult_non_generic_should_return_400_for_validation_failure()
    {
        var commandResult = CreateNonGenericFailureResult(
            new ValidationFailure( "Name", "Required." )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status400BadRequest, problem.StatusCode );
    }

    // ToFileResult tests

    [TestMethod]
    public void ToFileResult_should_return_file_for_valid_stream()
    {
        var stream = new MemoryStream( "test content"u8.ToArray() );
        var commandResult = CreateSuccessResult<Stream>( stream );

        var result = commandResult.ToFileResult( "application/pdf" );

        Assert.IsInstanceOfType( result, typeof( FileStreamHttpResult ) );
    }

    [TestMethod]
    public void ToFileResult_should_use_custom_content_type()
    {
        var stream = new MemoryStream( "test"u8.ToArray() );
        var commandResult = CreateSuccessResult<Stream>( stream );

        var result = commandResult.ToFileResult( "text/csv" );

        Assert.IsInstanceOfType( result, typeof( FileStreamHttpResult ) );
        var fileResult = (FileStreamHttpResult) result;
        Assert.AreEqual( "text/csv", fileResult.ContentType );
    }

    [TestMethod]
    public void ToFileResult_should_return_404_for_not_found_failure()
    {
        var commandResult = CreateFailureResult<Stream>(
            new NotFoundValidationFailure( "Report", "Report not found." )
        );

        var result = commandResult.ToFileResult( "application/pdf" );

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status404NotFound, problem.StatusCode );
    }
}
