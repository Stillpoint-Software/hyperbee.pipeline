using Hyperbee.Pipeline;
using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.Middleware;

public static partial class MiddlewareExtensions
{
    /// <summary>
    /// Adds exception handling to the pipeline, allowing specific exception types to be mapped to error codes.
    /// </summary>
    /// <remarks>This method allows you to define custom handling for specific exception types within the
    /// pipeline.  If an exception occurs during pipeline execution and it matches a configured exception type, the
    /// pipeline  will fail gracefully with the specified error code. If no matching configuration is found, the
    /// exception  will be re-thrown.</remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder to which exception handling will be added.</param>
    /// <param name="configure">A delegate to configure exception handling behavior. Use this to specify how exceptions should be mapped to
    /// error codes.</param>
    /// <returns>The pipeline builder with exception handling applied, allowing further configuration or execution.</returns>
    public static IPipelineStartBuilder<TInput, TOutput> WithExceptionHandling<TInput, TOutput>(
        this IPipelineStartBuilder<TInput, TOutput> builder,
        Action<ExceptionHandlingConfiguration> configure
    )
    {
        var config = new ExceptionHandlingConfiguration();
        configure( config );

        return builder.HookAsync(
            async ( context, argument, next ) =>
            {
                try
                {
                    var result = await next( context, argument ).ConfigureAwait( false );
                    return result;
                }
                catch ( Exception ex )
                {
                    if ( config.TryGetErrorCode( ex.GetType(), out var errorCode ) )
                    {
                        context.FailAfter( $"{ex.GetType().Name}: {ex.Message}", errorCode );
                        return default;
                    }

                    throw; // Re-throw if exception type not configured
                }
            }
        );
    }
}

public class ExceptionHandlingConfiguration
{
    private readonly Dictionary<Type, int> _exceptionMappings = [];

    internal ExceptionHandlingConfiguration() { }

    /// <summary>
    /// Maps a specific exception type to an error code for custom exception handling.
    /// </summary>
    /// <remarks>This method allows you to define custom mappings between exception types and error codes,
    /// which can be used to standardize error handling behavior across your application.</remarks>
    /// <typeparam name="TException">The type of exception to map. Must derive from <see cref="Exception"/>.</typeparam>
    /// <param name="errorcode">The error code to associate with the exception type. Defaults to -1 if not specified.</param>
    /// <returns>The current <see cref="ExceptionHandlingConfiguration"/> instance, allowing for method chaining.</returns>
    public ExceptionHandlingConfiguration AddException<TException>( int errorcode = -1 )
        where TException : Exception
    {
        _exceptionMappings[typeof( TException )] = errorcode;
        return this;
    }

    internal bool TryGetErrorCode( Type exceptionType, out int errorCode )
    {
        // Check for exact type match
        if ( _exceptionMappings.TryGetValue( exceptionType, out errorCode ) )
        {
            return true;
        }

        // Check for derived types
        foreach ( var kvp in _exceptionMappings )
        {
            if ( kvp.Key.IsAssignableFrom( exceptionType ) )
            {
                errorCode = kvp.Value;
                return true;
            }
        }

        errorCode = 0;
        return false;
    }
}
