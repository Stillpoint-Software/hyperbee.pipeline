using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Context;

public interface IPipelineContext
{
    CancellationToken CancellationToken { get; }

    ContextItems Items { get; }
    IServiceProvider ServiceProvider { get; init; }

    Exception Exception { get; set; }
    bool Throws { get; }

    bool Success { get; }
    bool IsError { get; }
    bool IsCanceled { get; }

    object CancellationValue { get; }
    bool HasCancellationValue { get; }

    void CancelAfter();
    void CancelAfter( object cancellationValue );
    IPipelineContext Clone( bool throws );

    string Name { get; set; }
    int Id { get; }

    ILogger Logger { get; }
}
