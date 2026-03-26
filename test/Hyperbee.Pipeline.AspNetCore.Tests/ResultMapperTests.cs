using Hyperbee.Pipeline.AspNetCore;
using Hyperbee.Pipeline.AspNetCore.Extensions;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hyperbee.Pipeline.AspNetCore.Tests;

[TestClass]
public class ResultMapperTests
{
    private static CommandResult<T> CreateErrorResult<T>( Exception exception )
    {
        var context = new PipelineContext { Exception = exception };
        return new CommandResult<T>
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };
    }

    private static CommandResult CreateNonGenericErrorResult( Exception exception )
    {
        var context = new PipelineContext { Exception = exception };
        return new CommandResult
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };
    }

    private static CommandResult<T> CreateSuccessResult<T>( T value )
    {
        var context = new PipelineContext();
        return new CommandResult<T>
        {
            Context = context,
            Result = value,
            CommandType = typeof( ResultMapperTests )
        };
    }

    // ResultMapper.Default behavior

    [TestMethod]
    public void Default_MapException_should_return_problem_details_500()
    {
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "something broke" )
        );

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status500InternalServerError, problem.StatusCode );
    }

    [TestMethod]
    public void Custom_MapException_returning_null_should_throw_CommandException()
    {
        var mapper = new RethrowMapper();
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "something broke" )
        );

        Assert.ThrowsExactly<CommandException>( () => commandResult.ToResult( mapper ) );
    }

    [TestMethod]
    public void Custom_MapException_should_handle_specific_exceptions()
    {
        var mapper = new ConflictMapper();
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict detected" )
        );

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void Custom_MapException_should_fall_through_to_base_for_unhandled()
    {
        var mapper = new ConflictMapper();
        var commandResult = CreateErrorResult<string>(
            new ArgumentException( "bad arg" )
        );

        var result = commandResult.ToResult( mapper );

        // Falls through to base.MapException which returns ProblemDetails 500
        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status500InternalServerError, problem.StatusCode );
    }

    // GetStatusCode defaults

    [TestMethod]
    public void Default_GetStatusCode_should_return_404_for_NotFoundValidationFailure()
    {
        var failure = new NotFoundValidationFailure( "Item", "Not found." );
        Assert.AreEqual( StatusCodes.Status404NotFound, ResultMapper.Default.GetStatusCode( failure ) );
    }

    [TestMethod]
    public void Default_GetStatusCode_should_return_403_for_ForbiddenValidationFailure()
    {
        var failure = new ForbiddenValidationFailure( "Resource", "Denied." );
        Assert.AreEqual( StatusCodes.Status403Forbidden, ResultMapper.Default.GetStatusCode( failure ) );
    }

    [TestMethod]
    public void Default_GetStatusCode_should_return_401_for_UnauthorizedValidationFailure()
    {
        var failure = new UnauthorizedValidationFailure( "User", "Unauthenticated." );
        Assert.AreEqual( StatusCodes.Status401Unauthorized, ResultMapper.Default.GetStatusCode( failure ) );
    }

    [TestMethod]
    public void Default_GetStatusCode_should_return_422_for_ApplicationValidationFailure()
    {
        var failure = new ApplicationValidationFailure( "Field", "Invalid." );
        Assert.AreEqual( StatusCodes.Status422UnprocessableEntity, ResultMapper.Default.GetStatusCode( failure ) );
    }

    [TestMethod]
    public void Default_GetStatusCode_should_return_422_for_base_ValidationFailure()
    {
        var failure = new ValidationFailure( "Field", "Required." );
        Assert.AreEqual( StatusCodes.Status422UnprocessableEntity, ResultMapper.Default.GetStatusCode( failure ) );
    }

    // MapSuccess defaults

    [TestMethod]
    public void Default_MapSuccess_should_return_ok_for_non_null()
    {
        var result = ResultMapper.Default.MapSuccess( "hello" );
        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void Default_MapSuccess_should_return_not_found_for_null()
    {
        var result = ResultMapper.Default.MapSuccess<string>( null! );
        Assert.IsInstanceOfType( result, typeof( NotFound ) );
    }

    // Generic ToResult with Func resultMapper

    [TestMethod]
    public void ToResult_with_func_resultMapper_should_return_mapped_result_on_error()
    {
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict detected" )
        );

        var result = commandResult.ToResult( cr =>
        {
            if ( cr.Context.IsError && cr.Context.Exception is InvalidOperationException ex &&
                 ex.Message.Contains( "version conflict" ) )
                return Results.Conflict( "Version conflict. Reload and retry." );
            return null;
        } );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_with_func_resultMapper_should_fall_through_to_default_on_success()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult( cr => null );

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void ToResult_with_func_resultMapper_should_allow_success_override()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult( cr =>
        {
            if ( cr.Context.Success )
                return Results.NoContent();
            return null;
        } );

        Assert.IsInstanceOfType( result, typeof( NoContent ) );
    }

    // Non-generic ToResult with Func resultMapper

    [TestMethod]
    public void ToResult_non_generic_with_func_resultMapper_should_return_mapped_result()
    {
        var commandResult = CreateNonGenericErrorResult(
            new InvalidOperationException( "version conflict" )
        );

        var result = commandResult.ToResult( cr =>
        {
            if ( cr.Context.IsError && cr.Context.Exception is InvalidOperationException )
                return Results.Conflict( "Version conflict." );
            return null;
        } );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_non_generic_with_func_resultMapper_should_fall_through_on_success()
    {
        var commandResult = new CommandResult
        {
            Context = new PipelineContext(),
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( cr => null );

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }

    // ResultMapper subclass tests

    [TestMethod]
    public void ToResult_with_ResultMapper_should_return_mapped_result_on_error()
    {
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict detected" )
        );
        var mapper = new ConflictMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_with_ResultMapper_should_fall_through_to_default_on_success()
    {
        var commandResult = CreateSuccessResult( "hello" );
        var mapper = new ConflictMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void ToResult_non_generic_with_ResultMapper_should_return_mapped_result()
    {
        var commandResult = CreateNonGenericErrorResult(
            new InvalidOperationException( "version conflict detected" )
        );
        var mapper = new ConflictMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_non_generic_with_ResultMapper_should_fall_through_on_success()
    {
        var commandResult = new CommandResult
        {
            Context = new PipelineContext(),
            CommandType = typeof( ResultMapperTests )
        };
        var mapper = new ConflictMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }

    // Cancellation tests

    [TestMethod]
    public void ToResult_should_return_cancellation_value_when_canceled_with_IResult()
    {
        var context = new PipelineContext();
        context.CancelAfter( Results.StatusCode( StatusCodes.Status408RequestTimeout ) );
        var commandResult = new CommandResult<string>
        {
            Context = context,
            Result = "hello",
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( StatusCodeHttpResult ) );
    }

    [TestMethod]
    public void ToResult_should_fall_through_to_success_when_canceled_without_IResult()
    {
        var context = new PipelineContext();
        context.CancelAfter( "not an IResult" );
        var commandResult = new CommandResult<string>
        {
            Context = context,
            Result = "hello",
            CommandType = typeof( ResultMapperTests )
        };

        // MapCancellation returns null for non-IResult, falls through to success
        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void Custom_MapCancellation_should_override_default()
    {
        var mapper = new TimeoutCancellationMapper();
        var context = new PipelineContext();
        context.CancelAfter();
        var commandResult = new CommandResult<string>
        {
            Context = context,
            Result = "hello",
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status408RequestTimeout, problem.StatusCode );
    }

    // Validation priority tests

    [TestMethod]
    public void ToResult_should_use_highest_priority_status_code_for_mixed_failures()
    {
        var context = new PipelineContext();
        var failures = new List<ValidationFailure>
        {
            new ApplicationValidationFailure( "Field", "Invalid." ),
            new NotFoundValidationFailure( "Item", "Not found." )
        };
        context.SetValidationResult(
            (IReadOnlyList<ValidationFailure>) failures,
            ValidationAction.CancelAfter
        );
        var commandResult = new CommandResult<string>
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult();

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status404NotFound, problem.StatusCode );
    }

    // Custom MapValidationFailures override

    [TestMethod]
    public void Custom_MapValidationFailures_should_override_problem_details_format()
    {
        var mapper = new CustomValidationMapper();
        var context = new PipelineContext();
        context.SetValidationResult(
            (IReadOnlyList<ValidationFailure>) new List<ValidationFailure>
            {
                new( "Name", "Required." )
            },
            ValidationAction.CancelAfter
        );
        var commandResult = new CommandResult<string>
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( JsonHttpResult<object> ) );
    }

    // ContentSelector + mapper on error path

    [TestMethod]
    public void ToResult_with_contentSelector_and_mapper_should_use_mapper_on_error()
    {
        var mapper = new ConflictMapper();
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict detected" )
        );

        var result = commandResult.ToResult( s => new { Value = s }, mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    // ToFileResult with custom mapper

    [TestMethod]
    public void ToFileResult_with_custom_mapper_should_use_mapper_on_error()
    {
        var mapper = new ConflictMapper();
        var context = new PipelineContext
        {
            Exception = new InvalidOperationException( "version conflict detected" )
        };
        var commandResult = new CommandResult<Stream>
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToFileResult( "application/pdf", mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    // ResultMapper.Create lambda factory tests

    [TestMethod]
    public void Create_with_mapException_should_handle_specific_exception()
    {
        var mapper = ResultMapper.Create(
            mapException: ex => ex is InvalidOperationException
                ? Results.Conflict( "Conflict." )
                : null
        );
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "conflict" )
        );

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void Create_with_mapException_returning_null_should_rethrow()
    {
        var mapper = ResultMapper.Create(
            mapException: _ => null
        );
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "boom" )
        );

        Assert.ThrowsExactly<CommandException>( () => commandResult.ToResult( mapper ) );
    }

    [TestMethod]
    public void Create_with_no_lambdas_should_use_defaults()
    {
        var mapper = ResultMapper.Create();
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void Create_with_getStatusCode_should_override_status()
    {
        var mapper = ResultMapper.Create(
            getStatusCode: _ => StatusCodes.Status400BadRequest
        );
        var context = new PipelineContext();
        context.SetValidationResult(
            (IReadOnlyList<ValidationFailure>) new List<ValidationFailure>
            {
                new( "Field", "Bad." )
            },
            ValidationAction.CancelAfter
        );
        var commandResult = new CommandResult<string>
        {
            Context = context,
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( ProblemHttpResult ) );
        var problem = (ProblemHttpResult) result;
        Assert.AreEqual( StatusCodes.Status400BadRequest, problem.StatusCode );
    }

    // GetPriorityStatusCode tests

    [TestMethod]
    public void GetPriorityStatusCode_should_pick_404_over_422()
    {
        var failures = new IValidationFailure[]
        {
            new ApplicationValidationFailure( "Field", "Invalid." ),
            new NotFoundValidationFailure( "Item", "Not found." )
        };

        var statusCode = ResultMapper.Default.GetPriorityStatusCode( failures );

        Assert.AreEqual( StatusCodes.Status404NotFound, statusCode );
    }

    [TestMethod]
    public void GetPriorityStatusCode_should_pick_403_over_401()
    {
        var failures = new IValidationFailure[]
        {
            new UnauthorizedValidationFailure( "User", "Unauthenticated." ),
            new ForbiddenValidationFailure( "Resource", "Denied." )
        };

        var statusCode = ResultMapper.Default.GetPriorityStatusCode( failures );

        Assert.AreEqual( StatusCodes.Status403Forbidden, statusCode );
    }

    [TestMethod]
    public void GetPriorityStatusCode_with_custom_mapper_should_respect_custom_codes()
    {
        var mapper = new Custom409Mapper();
        var failures = new IValidationFailure[]
        {
            new ApplicationValidationFailure( "Field", "Conflict." )
        };

        var statusCode = mapper.GetPriorityStatusCode( failures );

        Assert.AreEqual( StatusCodes.Status409Conflict, statusCode );
    }

    [TestMethod]
    public void GetPriorityStatusCode_should_use_lowest_for_unknown_custom_codes()
    {
        var mapper = new Custom409Mapper();
        var failures = new IValidationFailure[]
        {
            new ApplicationValidationFailure( "A", "Conflict." ),
            new ValidationFailure( "B", "Also conflict." )
        };

        // Both map to 409 via Custom409Mapper
        var statusCode = mapper.GetPriorityStatusCode( failures );

        Assert.AreEqual( StatusCodes.Status409Conflict, statusCode );
    }

    // Non-generic MapSuccess override tests

    [TestMethod]
    public void Non_generic_ToResult_should_use_MapSuccess()
    {
        var mapper = new NoContentSuccessMapper();
        var commandResult = new CommandResult
        {
            Context = new PipelineContext(),
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( NoContent ) );
    }

    [TestMethod]
    public void Default_non_generic_MapSuccess_should_return_ok()
    {
        var result = ResultMapper.Default.MapSuccess();

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }

    // Test helpers

    private class Custom409Mapper : ResultMapper
    {
        public override int GetStatusCode( IValidationFailure failure ) => failure switch
        {
            NotFoundValidationFailure => StatusCodes.Status404NotFound,
            ForbiddenValidationFailure => StatusCodes.Status403Forbidden,
            UnauthorizedValidationFailure => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status409Conflict
        };
    }

    private class NoContentSuccessMapper : ResultMapper
    {
        public override IResult MapSuccess() => Results.NoContent();
    }

    private class TimeoutCancellationMapper : ResultMapper
    {
        public override IResult? MapCancellation( object? cancellationValue )
        {
            return Results.Problem(
                detail: "The operation was canceled.",
                statusCode: StatusCodes.Status408RequestTimeout
            );
        }
    }

    private class CustomValidationMapper : ResultMapper
    {
        public override IResult MapValidationFailures( IEnumerable<IValidationFailure> failures, int statusCode )
        {
            return Results.Json( (object) new { errors = failures.Select( f => f.ErrorMessage ).ToList() }, statusCode: statusCode );
        }
    }

    private class RethrowMapper : ResultMapper
    {
        public override IResult? MapException( Exception exception ) => null;
    }

    private class ConflictMapper : ResultMapper
    {
        public override IResult? MapException( Exception exception ) => exception switch
        {
            InvalidOperationException ex when ex.Message.Contains( "version conflict" )
                => Results.Conflict( "Version conflict. Reload and retry." ),
            _ => base.MapException( exception )
        };
    }
}
