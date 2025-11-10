using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Context;

public class PipelineContextFactory : IPipelineContextFactory
{
    private readonly IServiceProvider _serviceProvider;

    private PipelineContextFactory( IServiceProvider serviceProvider )
    {
        // private instantiation guarantees a single instance.
        // this is important so that both DI and manual (non-DI) usage
        // use the same instance.

        _serviceProvider = serviceProvider;
    }

    public IPipelineContext Create( ILogger logger, CancellationToken cancellation = default )
    {
        return new PipelineContext( cancellation )
        {
            Logger = logger,
            ServiceProvider = _serviceProvider
        };
    }

    public static IPipelineContextFactory Instance { get; private set; }

    public static IPipelineContextFactory CreateFactory( IServiceProvider serviceProvider = null, bool resetFactory = false )
    {
        if ( resetFactory )
        {
            return Instance = new PipelineContextFactory( serviceProvider );
        }

        return Instance ??= new PipelineContextFactory( serviceProvider );
    }
}
