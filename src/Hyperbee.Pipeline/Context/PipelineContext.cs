using Microsoft.Extensions.Logging;

namespace Hyperbee.Pipeline.Context;

public class PipelineContext : IPipelineContext, IPipelineContextControl
{
    public PipelineContext()
        : this( default )
    {
    }

    public PipelineContext( CancellationToken cancellation )
    {
        CancellationSource = cancellation == default
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource( cancellation );

        Items = new();
        _counter = new Counter();
    }

    protected PipelineContext( PipelineContext source, bool throws )
    {
        CancellationSource = CancellationTokenSource.CreateLinkedTokenSource( source.CancellationToken );

        _counter = source._counter;

        Items = source.Items;
        Logger = source.Logger;
        ServiceProvider = source.ServiceProvider;
        Throws = throws;
    }

    private object _cancellationValue;
    private readonly Counter _counter; 

    private sealed class Counter // create a shared counter instance to support context parallelism
    {
        private int _nextId = 1;
        public int GetNextId() => Interlocked.Increment( ref _nextId );
    }

    protected CancellationTokenSource CancellationSource { get; }
    public CancellationToken CancellationToken => CancellationSource.Token;

    object IPipelineContextControl.CancellationValue
    {
        get => CancellationValue;
        set => CancellationValue = value;
    }

    public object CancellationValue
    {
        get => _cancellationValue;
        private set
        {
            if ( HasCancellationValue )
                throw new InvalidOperationException( $"{nameof(CancellationValue)} has already been set." );

            _cancellationValue = value;
            HasCancellationValue = true;
        }
    }

    public bool HasCancellationValue { get; private set; }

    public ContextItems Items { get; }
    public IServiceProvider ServiceProvider { get; init; }
    
    public Exception Exception { get; set; }
    public bool Throws { get; }

    public bool Success => !IsError && !IsCanceled;
    public bool IsError => Exception != null;
    public bool IsCanceled => CancellationSource.IsCancellationRequested;

    public ILogger Logger { get; init; }

    public string Name { get; set; } // set must be public to allow user to configure

    public int Id
    {
        get;
        private set;
    }

    string IPipelineContextControl.Name
    {
        get => Name;
        set => Name = value;
    }

    int IPipelineContextControl.Id
    {
        get => Id;
        set => Id = value;
    }
    
    public void CancelAfter() => CancellationSource.Cancel();

    public void CancelAfter( object cancellationValue )
    {
        CancellationSource.Cancel();
        CancellationValue = cancellationValue;
    }

    public IPipelineContext Clone( bool throws = false )
    {
        return new PipelineContext( this, throws );
    }

    int IPipelineContextControl.GetNextId() => _counter.GetNextId();
}