using FluentValidation;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.TestHelpers;

/// <summary>
/// Builder for configuring IPipelineContextFactory with validators and context services.
/// Uses AsyncLocal to provide test-scoped isolation when working with static factory.
/// </summary>
public class PipelineContextFactoryFixture
{
    private static readonly AsyncLocal<ContextState> _asyncLocalState = new();

    private readonly string _contextId;

    public PipelineContextFactoryFixture()
    {
        // Each fixture gets a unique context ID
        _contextId = Guid.NewGuid().ToString("N");

        // Initialize AsyncLocal state for this fixture
        _asyncLocalState.Value = new ContextState
        {
            ContextId = _contextId,
            Services = new ServiceCollection(),
            ValidatorProvider = new TestValidatorProvider(),
        };
    }

    /// <summary>
    /// Registers a validator for the specified model type.
    /// </summary>
    public PipelineContextFactoryFixture WithValidator<TModel, TValidator>()
        where TModel : class
        where TValidator : IValidator<TModel>, new()
    {
        var state = GetState();
        state.ValidatorProvider.Register(new TValidator());
        return this;
    }

    /// <summary>
    /// Registers a validator instance for the specified model type.
    /// </summary>
    public PipelineContextFactoryFixture WithValidator<TModel>(IValidator<TModel> validator)
        where TModel : class
    {
        var state = GetState();
        state.ValidatorProvider.Register(validator);
        return this;
    }

    /// <summary>
    /// Configures the service collection for DI setup.
    /// </summary>
    public PipelineContextFactoryFixture WithServices(Action<IServiceCollection> configure)
    {
        var state = GetState();
        configure(state.Services);
        return this;
    }

    /// <summary>
    /// Creates the IPipelineContextFactory with all configured services.
    /// Uses AsyncLocal to maintain test-scoped service provider isolation.
    /// </summary>
    public IPipelineContextFactory Create()
    {
        var state = GetState();

        // Register the validator provider as a singleton
        state.Services.AddSingleton<IValidatorProvider>(state.ValidatorProvider);

        // Build test-specific service provider
        var serviceProvider = state.Services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }
        );

        // Update AsyncLocal state with the built service provider
        state.ServiceProvider = serviceProvider;

        // Wrap the library's factory to use our AsyncLocal provider
        return new ConcurrentPipelineContextFactory(_contextId);
    }

    /// <summary>
    /// Creates and returns the default implementation of <see cref="IPipelineContextFactory"/>.
    /// </summary>
    /// <returns>An instance of <see cref="IPipelineContextFactory"/> configured with default settings.</returns>
    public static IPipelineContextFactory Default()
    {
        return new PipelineContextFactoryFixture().Create();
    }

    private ContextState GetState()
    {
        var state = _asyncLocalState.Value;
        if (state == null || state.ContextId != _contextId)
        {
            throw new InvalidOperationException(
                $"AsyncLocal context not found or mismatched. Expected: {_contextId}, Found: {state?.ContextId ?? "null"}"
            );
        }

        return state;
    }

    /// <summary>
    /// Wrapper factory that retrieves service provider from AsyncLocal context.
    /// </summary>
    private sealed class ConcurrentPipelineContextFactory(string contextId) : IPipelineContextFactory
    {
        public IPipelineContext Create(ILogger logger, CancellationToken cancellation = default)
        {
            // Retrieve the test-scoped service provider from AsyncLocal
            var state = _asyncLocalState.Value;

            if (state == null || state.ContextId != contextId)
            {
                throw new InvalidOperationException(
                    $"AsyncLocal context not found or mismatched. Expected: {contextId}, Found: {state?.ContextId ?? "null"}"
                );
            }

            if (state.ServiceProvider == null)
            {
                throw new InvalidOperationException(
                    "ServiceProvider has not been built. Call Create() on the fixture first."
                );
            }

            // Use the library's factory with our test-scoped provider
            var factory = PipelineContextFactory.CreateFactory(state.ServiceProvider, resetFactory: true);
            return factory.Create(logger ?? NullLogger.Instance, cancellation);
        }
    }

    /// <summary>
    /// Holds test-scoped state in AsyncLocal.
    /// </summary>
    private sealed class ContextState
    {
        public string ContextId { get; init; } = null!;
        public ServiceCollection Services { get; init; } = null!;
        public TestValidatorProvider ValidatorProvider { get; init; } = null!;
        public IServiceProvider? ServiceProvider { get; set; }
    }
}
