using System.Runtime.CompilerServices;
using Hyperbee.Pipeline;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides extension methods for adding validation to pipeline builders and contexts.
/// </summary>
/// <remarks>
/// <para>
/// This class integrates validation into pipeline execution flows, enabling declarative validation
/// of pipeline arguments with automatic error handling and pipeline cancellation on validation failures.
/// </para>
/// <para>
/// Validators are resolved from the service provider using <see cref="IValidatorProvider"/>, which must be
/// registered in the dependency injection container for validation to work.
/// </para>
/// <para>
/// All validation methods work with the abstracted <see cref="IValidator{T}"/> interface, making them
/// compatible with any validation framework that provides an adapter implementation.
/// </para>
/// </remarks>
public static class PipelineValidationExtensions
{
    private const string VALIDATION_RESULT_KEY = nameof( VALIDATION_RESULT_KEY );

    /// <summary>
    /// Adds an asynchronous validation step to the pipeline that validates the output argument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resolves an <see cref="IValidator{TOutput}"/> from the service provider using <see cref="IValidatorProvider"/>.
    /// If validation fails, the validation result is stored in the pipeline context and the pipeline is cancelled.
    /// </para>
    /// <para>
    /// If no validator is registered for <typeparamref name="TOutput"/>, or if <see cref="IValidatorProvider"/> is not
    /// registered in the service provider, a validation failure is recorded and the pipeline is cancelled.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline. Must be a reference type.</typeparam>
    /// <param name="builder">The pipeline builder to which the validation step is added.</param>
    /// <returns>The pipeline builder with the validation step added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var pipeline = PipelineBuilder
    ///     .Create&lt;CreateUserInput, User&gt;()
    ///     .ValidateAsync()
    ///     .Pipe((context, input) => CreateUser(input));
    /// </code>
    /// </example>
    public static IPipelineBuilder<TInput, TOutput> ValidateAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> builder
    )
        where TOutput : class
    {
        ArgumentNullException.ThrowIfNull( builder );

        return builder.CreateValidationFunction( null, false );
    }

    /// <summary>
    /// Adds an asynchronous validation step to the pipeline that validates the output argument using a specific RuleSet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resolves an <see cref="IValidator{TOutput}"/> from the service provider and validates using the specified
    /// RuleSet. RuleSets allow you to group validation rules within a single validator and execute them selectively based on the scenario.
    /// </para>
    /// <para>
    /// Multiple RuleSets can be specified by separating them with commas: "Create,Common".
    /// Use "*" to include all RuleSets.
    /// If the RuleSet is null or empty, only the default rules (those not in any RuleSet) are executed.
    /// </para>
    /// <para>
    /// If validation fails, the validation result is stored in the pipeline context and the pipeline is cancelled.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline. Must be a reference type.</typeparam>
    /// <param name="builder">The pipeline builder to which the validation step is added.</param>
    /// <param name="ruleSet">
    /// The RuleSet name(s) to execute. Multiple RuleSets can be specified by separating them with commas.
    /// Use "*" to include all RuleSets. If null or empty, only default rules are executed.
    /// </param>
    /// <param name="includeDefaultRules">
    /// When true, executes default rules (those not in any RuleSet) in addition to the specified RuleSet.
    /// Defaults to true.
    /// </param>
    /// <returns>The pipeline builder with the validation step added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// // Single validator with RuleSets for different scenarios
    /// public class ProductCatalogValidator : AbstractValidator&lt;ProductCatalog&gt;
    /// {
    ///     public ProductCatalogValidator()
    ///     {
    ///         // Always executed rules
    ///         RuleFor(x => x.Name).NotEmpty();
    ///         RuleFor(x => x.Quantity).GreaterThan(0);
    ///
    ///         // Create scenario - ID and version not required
    ///         RuleSet("Create", () =>
    ///         {
    ///             RuleFor(x => x.Quantity).LessThanOrEqualTo(1000);
    ///         });
    ///
    ///         // Update scenario - ID and version required
    ///         RuleSet("Update", () =>
    ///         {
    ///             RuleFor(x => x.Id).NotEmpty();
    ///             RuleFor(x => x.VersionTag).NotEmpty();
    ///         });
    ///     }
    /// }
    ///
    /// // Create command pipeline
    /// var createPipeline = PipelineBuilder
    ///     .Create&lt;CreateInput, ProductCatalog&gt;()
    ///     .ValidateAsync("Create")  // Uses Create RuleSet + default rules
    ///     .Pipe((context, input) => CreateItem(input));
    ///
    /// // Update command pipeline
    /// var updatePipeline = PipelineBuilder
    ///     .Create&lt;UpdateInput, ProductCatalog&gt;()
    ///     .ValidateAsync("Update")  // Uses Update RuleSet + default rules
    ///     .Pipe((context, input) => UpdateItem(input));
    /// </code>
    /// </example>
    public static IPipelineBuilder<TInput, TOutput> ValidateAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> builder,
        string? ruleSet,
        bool includeDefaultRules = true
    )
        where TOutput : class
    {
        ArgumentNullException.ThrowIfNull( builder );

        return builder.CreateValidationFunction( ( _, _ ) => ruleSet, includeDefaultRules );
    }

    /// <summary>
    /// Adds an asynchronous validation step with a dynamic RuleSet selector based on pipeline context or data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method allows you to dynamically determine which RuleSet(s) to execute based on the pipeline
    /// context and/or the data being validated. This is useful when the validation scenario depends on runtime conditions.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline. Must be a reference type.</typeparam>
    /// <param name="builder">The pipeline builder to which the validation step is added.</param>
    /// <param name="ruleSetSelector">
    /// A function that determines which RuleSet(s) to execute based on the context and argument.
    /// Return null or empty string for default rules only, or a comma-separated list of RuleSet names.
    /// </param>
    /// <param name="includeDefaultRules">
    /// When true, executes default rules in addition to the selected RuleSet(s). Defaults to true.
    /// </param>
    /// <returns>The pipeline builder with the validation step added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="ruleSetSelector"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var pipeline = PipelineBuilder
    ///     .Create&lt;CreateUserInput, User&gt;()
    ///     .ValidateAsync((ctx, user) => user.IsAdmin ? "Admin" : "Standard")
    ///     .Pipe((context, input) => CreateUser(input));
    /// </code>
    /// </example>
    public static IPipelineBuilder<TInput, TOutput> ValidateAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> builder,
        Func<IPipelineContext, TOutput, string?> ruleSetSelector,
        bool includeDefaultRules = true
    )
        where TOutput : class
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( ruleSetSelector );

        return builder.CreateValidationFunction( ruleSetSelector, includeDefaultRules );
    }

    /// <summary>
    /// Adds a validation step to the pipeline that validates the output using a specified rule set.
    /// </summary>
    /// <remarks>If the <paramref name="ruleSetSelector"/> is <see langword="null"/> or returns a null or
    /// whitespace string, the default validation rules are applied. If no validator is registered for the output type,
    /// the pipeline context is marked as failed.</remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output to be validated, which must be a class.</typeparam>
    /// <param name="builder">The pipeline builder to which the validation step is added.</param>
    /// <param name="ruleSetSelector">A function that selects the rule set to be used for validation based on the pipeline context and output. Can be
    /// <see langword="null"/>.</param>
    /// <param name="includeDefaultRules">A value indicating whether to include default rules in the validation. Defaults to <see langword="true"/>.</param>
    /// <returns>The pipeline builder with the validation step added.</returns>
    private static IPipelineBuilder<TInput, TOutput> CreateValidationFunction<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> builder,
        Func<IPipelineContext, TOutput, string?>? ruleSetSelector,
        bool includeDefaultRules = true
    )
        where TOutput : class
    {
        return builder.PipeAsync(
            async ( context, argument ) =>
            {
                if ( argument is not null )
                    await context.ValidateAsync( argument, ruleSetSelector ?? (( _, _ ) => null), includeDefaultRules )
                        .ConfigureAwait( false );

                return argument!;
            }
        );
    }

    // context.ValidateAsync

    /// <summary>
    /// Imperatively validates the specified argument using the registered validator, storing the result in the context.
    /// </summary>
    /// <typeparam name="T">The type of the argument to validate. Must be a reference type.</typeparam>
    /// <param name="context">The pipeline context.</param>
    /// <param name="argument">The argument to validate.</param>
    /// <returns><see langword="true"/> if validation succeeds; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> ValidateAsync<T>( this IPipelineContext context, T argument )
        where T : class
    {
        if ( argument is null )
            return true;

        var result = await ValidateCoreAsync( context, argument ).ConfigureAwait( false );

        if ( !result.IsValid )
            context.SetValidationResult( result, ValidationAction.CancelAfter );

        return context.IsValid();
    }

    /// <summary>
    /// Imperatively validates the specified argument using the registered validator with a specific RuleSet.
    /// </summary>
    /// <typeparam name="T">The type of the argument to validate. Must be a reference type.</typeparam>
    /// <param name="context">The pipeline context.</param>
    /// <param name="argument">The argument to validate.</param>
    /// <param name="ruleSet">The RuleSet name(s) to execute.</param>
    /// <param name="includeDefaultRules">When true, executes default rules in addition to the specified RuleSet. Defaults to true.</param>
    /// <returns><see langword="true"/> if validation succeeds; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> ValidateAsync<T>( this IPipelineContext context, T argument, string? ruleSet, bool includeDefaultRules = true )
        where T : class
    {
        if ( argument is null )
            return true;

        var result = await ValidateCoreAsync( context, argument, ruleSet, includeDefaultRules ).ConfigureAwait( false );

        if ( !result.IsValid )
            context.SetValidationResult( result, ValidationAction.CancelAfter );

        return context.IsValid();
    }

    /// <summary>
    /// Imperatively validates the specified argument using the registered validator with a dynamic RuleSet selector.
    /// </summary>
    /// <typeparam name="T">The type of the argument to validate. Must be a reference type.</typeparam>
    /// <param name="context">The pipeline context.</param>
    /// <param name="argument">The argument to validate.</param>
    /// <param name="ruleSetSelector">A function that determines which RuleSet(s) to execute.</param>
    /// <param name="includeDefaultRules">When true, executes default rules in addition to the selected RuleSet(s). Defaults to true.</param>
    /// <returns><see langword="true"/> if validation succeeds; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> ValidateAsync<T>( this IPipelineContext context, T argument, Func<IPipelineContext, T, string?> ruleSetSelector, bool includeDefaultRules = true )
        where T : class
    {
        ArgumentNullException.ThrowIfNull( ruleSetSelector );

        if ( argument is null )
            return true;

        var ruleSet = ruleSetSelector( context, argument );
        var result = await ValidateCoreAsync( context, argument, ruleSet, includeDefaultRules ).ConfigureAwait( false );

        if ( !result.IsValid )
            context.SetValidationResult( result, ValidationAction.CancelAfter );

        return context.IsValid();
    }

    // core

    internal static async Task<IValidationResult> ValidateCoreAsync<T>(
        IPipelineContext context,
        T argument,
        string? ruleSet = null,
        bool includeDefaultRules = true
    )
        where T : class
    {
        var provider = context.ServiceProvider.GetService<IValidatorProvider>();
        if ( provider == null )
        {
            return new ValidationResult( [new ValidationFailure( typeof( T ).Name, $"'{nameof( IValidatorProvider )}' not registered in service provider" )] );
        }

        var validator = provider.For<T>();
        if ( validator == null )
        {
            return new ValidationResult( [new ValidationFailure( typeof( T ).Name, $"No validator registered for type '{typeof( T ).Name}'" )] );
        }

        if ( string.IsNullOrWhiteSpace( ruleSet ) )
        {
            return await validator.ValidateAsync( argument, context.CancellationToken ).ConfigureAwait( false );
        }

        return await validator.ValidateAsync(
            argument,
            options =>
            {
                if ( includeDefaultRules )
                    options.IncludeRuleSets( ["default", .. ruleSet.Split( ',' ).Select( s => s.Trim() )] );
                else
                    options.IncludeRuleSets( [.. ruleSet.Split( ',' ).Select( s => s.Trim() )] );
            },
            context.CancellationToken
        ).ConfigureAwait( false );
    }

    // fail helper

    /// <summary>
    /// Records a pre-built validation failure and automatically cancels pipeline execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="validationFailure">The validation failure to record.</param>
    public static void FailAfter(
        this IPipelineContext context,
        IValidationFailure validationFailure
    )
    {
        context.SetValidationResult( validationFailure, ValidationAction.CancelAfter );
    }

    /// <summary>
    /// Records a validation failure with an error code and automatically cancels pipeline execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="message">The error message describing the validation failure.</param>
    /// <param name="code">The error code to associate with the validation failure.</param>
    /// <param name="propertyName">
    /// The name of the property that failed validation. When called from a property setter or method,
    /// this parameter is automatically populated with the caller member name.
    /// </param>
    public static void FailAfter(
        this IPipelineContext context,
        string message,
        int code,
        [CallerMemberName] string propertyName = default!
    )
    {
        context.SetValidationResult(
            ValidationFailure.Create( propertyName!, message, code.ToString() ),
            ValidationAction.CancelAfter
        );
    }

    /// <summary>
    /// Records a validation failure and automatically cancels pipeline execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="message">The error message describing the validation failure.</param>
    /// <param name="propertyName">
    /// The name of the property that failed validation. When called from a property setter or method,
    /// this parameter is automatically populated with the caller member name.
    /// </param>
    public static void FailAfter(
        this IPipelineContext context,
        string message,
        [CallerMemberName] string propertyName = default!
    )
    {
        context.SetValidationResult( ValidationFailure.Create( propertyName!, message ), ValidationAction.CancelAfter );
    }

    // validation result

    /// <summary>
    /// Retrieves the validation result stored in the pipeline context, if any.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>
    /// The <see cref="IValidationResult"/> stored in the context, or <see langword="null"/> if no validation result has been set.
    /// </returns>
    public static IValidationResult? GetValidationResult( this IPipelineContext context )
    {
        return context.Items.TryGetValue<IValidationResult>( VALIDATION_RESULT_KEY, out var item ) ? item : default;
    }

    /// <summary>
    /// Sets the validation result in the pipeline context and optionally cancels pipeline execution.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="validationResult">The validation result to store in the context.</param>
    /// <param name="validationAction">
    /// The action to take after setting the validation result. Defaults to <see cref="ValidationAction.ContinueAfter"/>.
    /// </param>
    public static void SetValidationResult(
        this IPipelineContext context,
        IValidationResult validationResult,
        ValidationAction validationAction = ValidationAction.ContinueAfter
    )
    {
        context.Items.SetValue( VALIDATION_RESULT_KEY, validationResult );

        if ( validationAction == ValidationAction.CancelAfter )
        {
            context.CancelAfter();
        }
    }

    /// <summary>
    /// Creates a validation result from a list of validation failures and stores it in the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="validationFailures">The list of validation failures.</param>
    /// <param name="validationAction">
    /// The action to take after setting the validation result. Defaults to <see cref="ValidationAction.ContinueAfter"/>.
    /// </param>
    public static void SetValidationResult(
        this IPipelineContext context,
        IReadOnlyList<IValidationFailure> validationFailures,
        ValidationAction validationAction = ValidationAction.ContinueAfter
    )
    {
        var validationResult = new ValidationResult( validationFailures );

        context.SetValidationResult( validationResult, validationAction );
    }

    /// <summary>
    /// Creates a validation result from a single validation failure and stores it in the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="validationFailure">The validation failure.</param>
    /// <param name="validationAction">
    /// The action to take after setting the validation result. Defaults to <see cref="ValidationAction.ContinueAfter"/>.
    /// </param>
    public static void SetValidationResult(
        this IPipelineContext context,
        IValidationFailure validationFailure,
        ValidationAction validationAction = ValidationAction.ContinueAfter
    )
    {
        context.SetValidationResult( [validationFailure], validationAction );
    }

    /// <summary>
    /// Adds a validation failure to the existing validation result in the pipeline context, or creates a new one if none exists.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="validationFailure">The validation failure to add.</param>
    /// <param name="validationAction">
    /// The action to take after adding the validation failure. Defaults to <see cref="ValidationAction.ContinueAfter"/>.
    /// </param>
    public static void AddValidationResult(
        this IPipelineContext context,
        IValidationFailure validationFailure,
        ValidationAction validationAction = ValidationAction.ContinueAfter
    )
    {
        if ( context.Items.TryGetValue<IValidationResult>( VALIDATION_RESULT_KEY, out var validationResult ) )
        {
            validationResult.Errors.Add( validationFailure );
            return;
        }

        context.SetValidationResult( [validationFailure], validationAction );
    }

    /// <summary>
    /// Clears the validation result from the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    public static void ClearValidationResult( this IPipelineContext context )
    {
        context.Items.SetValue<IValidationResult>( VALIDATION_RESULT_KEY, null! );
    }

    /// <summary>
    /// Determines whether the pipeline context contains a valid validation result (i.e., no validation errors).
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>
    /// <see langword="true"/> if no validation result exists or if the validation result indicates success;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValid( this IPipelineContext context )
    {
        return context.GetValidationResult()?.IsValid ?? true;
    }

    /// <summary>
    /// Retrieves all validation failures from the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>
    /// An enumerable collection of <see cref="IValidationFailure"/> instances, or an empty collection if no validation result exists.
    /// </returns>
    public static IEnumerable<IValidationFailure> ValidationFailures( this IPipelineContext context )
    {
        return context.GetValidationResult()?.Errors ?? Enumerable.Empty<IValidationFailure>();
    }

    // pipeline builder extensions

    /// <summary>
    /// Validates the pipeline output and conditionally executes a builder function only if validation succeeds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method first validates the output using <see cref="ValidateAsync{TInput, TOutput}(IPipelineBuilder{TInput, TOutput})"/>, then checks if the
    /// validation result is valid using <see cref="IsValid"/>. If valid, the provided builder function is executed;
    /// otherwise, the pipeline continues without executing the builder.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline. Must be a reference type.</typeparam>
    /// <param name="pipeline">The pipeline builder.</param>
    /// <param name="builder">A function that builds additional pipeline steps to execute when validation succeeds.</param>
    /// <returns>The pipeline builder with conditional validation logic added.</returns>
    public static IPipelineBuilder<TInput, TOutput> IfValidAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> pipeline,
        Func<IPipelineBuilder<TOutput, TOutput>, IPipelineBuilder> builder
    )
        where TOutput : class
    {
        // usage: .IfValidAsync(builder => ... )
        return pipeline.ValidateAsync().CallIf( ( c, _ ) => c.IsValid(), builder );
    }

    /// <summary>
    /// Validates the pipeline output and cancels pipeline execution if validation fails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience method that combines <see cref="ValidateAsync{TInput, TOutput}(IPipelineBuilder{TInput, TOutput})"/> with an automatic
    /// cancellation check. If validation fails, the pipeline is cancelled and no further steps are executed.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline. Must be a reference type.</typeparam>
    /// <param name="pipeline">The pipeline builder.</param>
    /// <returns>The pipeline builder with validation and automatic cancellation logic added.</returns>
    public static IPipelineBuilder<TInput, TOutput> ValidateAndCancelOnFailureAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> pipeline
    )
        where TOutput : class
    {
        // usage: .ValidateAndCancelOnFailureAsync()
        return pipeline
            .ValidateAsync()
            .Pipe(
                ( c, a ) =>
                {
                    if ( !c.IsValid() )
                    {
                        c.CancelAfter();
                    }

                    return a;
                }
            );
    }

    /// <summary>
    /// Cancels pipeline execution with a specific validation failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is useful for short-circuiting pipeline execution when a specific validation condition is not met.
    /// The provided validation failure is stored in the context and the pipeline is cancelled immediately.
    /// </para>
    /// </remarks>
    /// <typeparam name="TInput">The type of the input to the pipeline.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the pipeline.</typeparam>
    /// <param name="pipeline">The pipeline builder.</param>
    /// <param name="validationFailure">The validation failure to record before cancelling.</param>
    /// <returns>The pipeline builder with cancellation logic added.</returns>
    public static IPipelineBuilder<TInput, TOutput> CancelWithValidationResult<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> pipeline,
        IValidationFailure validationFailure
    )
    {
        // usage: .CancelWithValidationResult( validationFailure )
        return pipeline.Pipe(
            ( c, a ) =>
            {
                c.SetValidationResult( [validationFailure], ValidationAction.CancelAfter );
                return a;
            }
        );
    }
}
