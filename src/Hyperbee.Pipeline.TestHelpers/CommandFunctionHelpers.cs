using Hyperbee.Pipeline.Commands;
using NSubstitute;

namespace Hyperbee.Pipeline.TestHelpers;

/// <summary>
/// Provides extension methods for mocking ICommandFunction instances in unit tests.
/// </summary>
public static class CommandFunctionHelpers
{
    /// <summary>
    /// Mocks a command function to return a custom command result.
    /// </summary>
    /// <typeparam name="TStart">The input type for the command function.</typeparam>
    /// <typeparam name="TOutput">The output type for the command function.</typeparam>
    /// <param name="commandFunction">The command function to mock.</param>
    /// <param name="funcGetCommandResult">A function that returns the command result to use.</param>
    public static void MockCommandFunction<TStart, TOutput>( this ICommandFunction<TStart, TOutput> commandFunction, Func<CommandResult<TOutput>> funcGetCommandResult )
    {
        var commandResult = funcGetCommandResult();

        commandFunction.ExecuteAsync(
            NSubstitute.Arg.Any<TStart>(),
            NSubstitute.Arg.Any<CancellationToken>() )
            .Returns( commandResult );
    }

    /// <summary>
    /// Mocks a command function to return a successful result with the specified output.
    /// </summary>
    /// <typeparam name="TStart">The input type for the command function.</typeparam>
    /// <typeparam name="TOutput">The output type for the command function.</typeparam>
    /// <param name="commandFunction">The command function to mock.</param>
    /// <param name="output">The output value to return in the successful result.</param>
    public static void MockSuccessfulResult<TStart, TOutput>( this ICommandFunction<TStart, TOutput> commandFunction, TOutput output )
    {
        commandFunction.MockCommandFunction( () => CommandResultHelpers.CreateSuccess( output ) );
    }

    /// <summary>
    /// Mocks a command function to return a validation failure result with the specified validation failures.
    /// </summary>
    /// <typeparam name="TStart">The input type for the command function.</typeparam>
    /// <typeparam name="TOutput">The output type for the command function.</typeparam>
    /// <param name="commandFunction">The command function to mock.</param>
    /// <param name="failures">The validation failures to include in the result.</param>
    public static void MockValidationFailureResult<TStart, TOutput>( this ICommandFunction<TStart, TOutput> commandFunction, params FluentValidation.Results.ValidationFailure[] failures )
    {
        commandFunction.MockCommandFunction( () => CommandResultHelpers.CreateValidationFailure<TOutput>( failures ) );
    }

    /// <summary>
    /// Mocks a command function to return an exception result with the specified exception.
    /// </summary>
    /// <typeparam name="TStart">The input type for the command function.</typeparam>
    /// <typeparam name="TOutput">The output type for the command function.</typeparam>
    /// <param name="commandFunction">The command function to mock.</param>
    /// <param name="exception">The exception to include in the result.</param>
    public static void MockExceptionCommandResult<TStart, TOutput>( this ICommandFunction<TStart, TOutput> commandFunction, Exception exception )
    {
        commandFunction.MockCommandFunction( () => CommandResultHelpers.CreateWithException<TOutput>( exception ) );
    }
}
