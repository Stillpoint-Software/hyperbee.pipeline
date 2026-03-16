using Hyperbee.Pipeline.AspNetCore.Extensions;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
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

    // Generic ToResult with resultMapper

    [TestMethod]
    public void ToResult_with_resultMapper_should_return_mapped_result_on_error()
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
    public void ToResult_with_resultMapper_should_fall_through_to_default_on_success()
    {
        var commandResult = CreateSuccessResult( "hello" );

        var result = commandResult.ToResult( cr => null );

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void ToResult_with_resultMapper_should_allow_success_override()
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

    [TestMethod]
    public void ToResult_with_resultMapper_should_handle_combined_error_and_success()
    {
        var successResult = CreateSuccessResult( "hello" );
        var errorResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict" )
        );

        IResult? Mapper( CommandResult<string> cr )
        {
            if ( cr.Context.IsError && cr.Context.Exception is InvalidOperationException )
                return Results.Conflict( "Version conflict." );
            if ( cr.Context.Success )
                return Results.NoContent();
            return null;
        }

        var successIResult = successResult.ToResult( Mapper );
        var errorIResult = errorResult.ToResult( Mapper );

        Assert.IsInstanceOfType( successIResult, typeof( NoContent ) );
        Assert.IsInstanceOfType( errorIResult, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_with_resultMapper_should_fall_through_to_default_error_handling()
    {
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "some other error" )
        );

        // Mapper doesn't handle this error type — returns null, so default handling throws
        try
        {
            commandResult.ToResult( cr =>
            {
                if ( cr.Context.Exception is ArgumentException )
                    return Results.Conflict( "Handled." );
                return null;
            } );
            Assert.Fail( "Expected CommandException to be thrown." );
        }
        catch ( CommandException )
        {
            // Expected
        }
    }

    // Non-generic ToResult with resultMapper

    [TestMethod]
    public void ToResult_non_generic_with_resultMapper_should_return_mapped_result()
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
    public void ToResult_non_generic_with_resultMapper_should_fall_through_on_success()
    {
        var commandResult = new CommandResult
        {
            Context = new PipelineContext(),
            CommandType = typeof( ResultMapperTests )
        };

        var result = commandResult.ToResult( cr => null );

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }

    // IResultMapper interface tests

    private class ConflictResultMapper : IResultMapper
    {
        public IResult? Map( CommandResult commandResult )
        {
            if ( commandResult.Context.IsError &&
                 commandResult.Context.Exception is InvalidOperationException ex &&
                 ex.Message.Contains( "version conflict" ) )
            {
                return Results.Conflict( "Version conflict. Reload and retry." );
            }

            return null;
        }
    }

    [TestMethod]
    public void ToResult_with_IResultMapper_should_return_mapped_result_on_error()
    {
        var commandResult = CreateErrorResult<string>(
            new InvalidOperationException( "version conflict detected" )
        );
        var mapper = new ConflictResultMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_with_IResultMapper_should_fall_through_to_default_on_success()
    {
        var commandResult = CreateSuccessResult( "hello" );
        var mapper = new ConflictResultMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Ok<string> ) );
    }

    [TestMethod]
    public void ToResult_with_IResultMapper_should_fall_through_on_unhandled_error()
    {
        var commandResult = CreateErrorResult<string>( new ArgumentException( "bad arg" ) );
        var mapper = new ConflictResultMapper();

        try
        {
            commandResult.ToResult( mapper );
            Assert.Fail( "Expected CommandException to be thrown." );
        }
        catch ( CommandException )
        {
            // Expected — mapper returned null, default handling throws
        }
    }

    [TestMethod]
    public void ToResult_non_generic_with_IResultMapper_should_return_mapped_result()
    {
        var commandResult = CreateNonGenericErrorResult(
            new InvalidOperationException( "version conflict detected" )
        );
        var mapper = new ConflictResultMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Conflict<string> ) );
    }

    [TestMethod]
    public void ToResult_non_generic_with_IResultMapper_should_fall_through_on_success()
    {
        var commandResult = new CommandResult
        {
            Context = new PipelineContext(),
            CommandType = typeof( ResultMapperTests )
        };
        var mapper = new ConflictResultMapper();

        var result = commandResult.ToResult( mapper );

        Assert.IsInstanceOfType( result, typeof( Ok ) );
    }
}
