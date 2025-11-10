using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Context;

public interface IPipelineContextFactory
{
    IPipelineContext Create( ILogger logger, CancellationToken cancellation = default );
}
