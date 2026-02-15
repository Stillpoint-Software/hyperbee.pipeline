using FluentValidation.Results;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation;
using NSubstitute;

namespace Hyperbee.Pipeline.TestHelpers;

public static class CommandResultHelpers
{
    /// <summary>
    /// Creates a CommandResult with a successful validation result.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the command result.</typeparam>
    /// <param name="result">The result value to return.</param>
    /// <param name="commandType">The type of the command interface (optional, defaults to the output type).</param>
    /// <returns>A CommandResult with valid state.</returns>
    public static CommandResult<TOutput> CreateSuccess<TOutput>( TOutput result, Type? commandType = null )
    {
        var context = CreateContext();
        context.SetValidationResult( new ValidationResult() );

        return new CommandResult<TOutput>
        {
            Context = context,
            Result = result,
            CommandType = commandType ?? typeof( TOutput )
        };
    }

    /// <summary>
    /// Creates a CommandResult with validation errors.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the command result.</typeparam>
    /// <param name="failures">One or more validation failures.</param>
    /// <returns>A CommandResult with validation errors.</returns>
    public static CommandResult<TOutput> CreateValidationFailure<TOutput>( params ValidationFailure[] failures )
    {
        return CreateValidationFailure<TOutput>( null, failures );
    }

    /// <summary>
    /// Creates a CommandResult with validation errors.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the command result.</typeparam>
    /// <param name="commandType">The type of the command interface (optional, defaults to the output type).</param>
    /// <param name="failures">One or more validation failures.</param>
    /// <returns>A CommandResult with validation errors.</returns>
    public static CommandResult<TOutput> CreateValidationFailure<TOutput>( Type? commandType, params ValidationFailure[] failures )
    {
        var context = CreateContext();
        context.SetValidationResult( new ValidationResult( failures ) );

        return new CommandResult<TOutput>
        {
            Context = context,
            Result = default!,
            CommandType = commandType ?? typeof( TOutput )
        };
    }

    /// <summary>
    /// Creates a CommandResult with a custom validation result.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the command result.</typeparam>
    /// <param name="result">The result value to return.</param>
    /// <param name="validationResult">The validation result to set in the context.</param>
    /// <param name="commandType">The type of the command interface (optional, defaults to the output type).</param>
    /// <returns>A CommandResult with the specified validation state.</returns>
    public static CommandResult<TOutput> CreateWithValidationResult<TOutput>(
        TOutput result,
        ValidationResult validationResult,
        Type? commandType = null )
    {
        var context = CreateContext();
        context.SetValidationResult( validationResult );

        return new CommandResult<TOutput>
        {
            Context = context,
            Result = result,
            CommandType = commandType ?? typeof( TOutput )
        };
    }

    /// <summary>
    /// Creates a CommandResult with an exception set in the context.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the command result.</typeparam>
    /// <param name="exception">The exception to set in the context.</param>
    /// <param name="commandType">The type of the command interface (optional, defaults to the output type).</param>
    /// <returns>A CommandResult with an exception.</returns>
    public static CommandResult<TOutput> CreateWithException<TOutput>( Exception exception, Type? commandType = null )
    {
        var context = CreateContext();
        context.Exception.Returns( exception );

        return new CommandResult<TOutput>
        {
            Context = context,
            Result = default!,
            CommandType = commandType ?? typeof( TOutput )
        };
    }

    /// <summary>
    /// Creates a mock IPipelineContext with a real ContextItems instance.
    /// This allows the SetValidationResult and GetValidationResult extension methods to work properly.
    /// </summary>
    /// <returns>A mocked IPipelineContext with functional ContextItems.</returns>
    private static IPipelineContext CreateContext()
    {
        var context = Substitute.For<IPipelineContext>();

        // Create a real ContextItems instance using reflection since the constructor is internal
        var contextItemsType = typeof( ContextItems );
        var contextItems = (ContextItems) Activator.CreateInstance(
            contextItemsType,
            nonPublic: true )!;

        context.Items.Returns( contextItems );

        return context;
    }
}
