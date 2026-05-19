using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Context;

public class PipelineContextFactory : IPipelineContextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineOptions _options;

    private PipelineContextFactory( IServiceProvider serviceProvider, PipelineOptions options )
    {
        // private instantiation guarantees a single instance.
        // this is important so that both DI and manual (non-DI) usage
        // use the same instance.

        _serviceProvider = serviceProvider;
        _options = options ?? new PipelineOptions();
    }

    public IPipelineContext Create( ILogger logger, CancellationToken cancellation = default )
    {
        return new PipelineContext( cancellation )
        {
            Logger = logger,
            ServiceProvider = _serviceProvider,
            HaltOnError = _options.HaltOnError
        };
    }

    public static IPipelineContextFactory Instance { get; private set; }

    public static IPipelineContextFactory CreateFactory( IServiceProvider serviceProvider = null, bool resetFactory = false, PipelineOptions options = null )
    {
        if ( resetFactory )
        {
            return Instance = new PipelineContextFactory( serviceProvider, options );
        }

        return Instance ??= new PipelineContextFactory( serviceProvider, options );
    }
}
