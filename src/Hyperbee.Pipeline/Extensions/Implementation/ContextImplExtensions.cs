using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Extensions.Implementation;

public static class ContextImplExtensions
{
    public static bool HandleCancellationRequested<TOutput>( this IPipelineContextControl control, TOutput value )
    {
        if ( !control.CancellationToken.IsCancellationRequested )
            return false;

        if ( !control.HasCancellationValue )
            control.CancellationValue = value;

        return true;
    }

    public static IDisposable CreateFrame( this IPipelineContextControl control, IPipelineContext context, Action<IPipelineContext> configure, string defaultName = null )
    {
        var name = context.Name;
        var id = context.Id;

        try
        {
            control.Id = control.GetNextId();
            control.Name = defaultName;

            configure?.Invoke( context ); // invoke user configure

            return new Disposable( () =>
            {
                control.Id = id;
                control.Name = name;
            } );
        }
        catch
        {
            control.Id = id;
            control.Name = name;
            throw;
        }
    }

    private sealed class Disposable( Action dispose ) : IDisposable
    {
        private int _disposed;
        private Action Disposer { get; } = dispose;

        public void Dispose()
        {
            if ( Interlocked.CompareExchange( ref _disposed, 1, 0 ) == 0 )
                Disposer.Invoke();
        }
    }
}