using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;

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

    public static Expression CreateFrameExpression(
        Expression context,
        Expression<Action<IPipelineContext>> config,
        string defaultName = null
    )
    {
        if ( config == null )
            return Call( ConfigureMethodInfo, arguments: [context, Constant( null, typeof( Action<IPipelineContext> ) ), Constant( defaultName )] );

        return Call( ConfigureMethodInfo, arguments: [context, config, Constant( defaultName )] );
    }

    private static MethodInfo ConfigureMethodInfo => typeof( ContextImplExtensions ).GetMethod( nameof( Configure ), BindingFlags.NonPublic | BindingFlags.Static );
    private static IDisposable Configure( IPipelineContext context, Action<IPipelineContext> configure, string defaultName )
    {
        var name = context.Name;
        var id = context.Id;

        var control = context as IPipelineContextControl;

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
        public static readonly ConstructorInfo ConstructorInfo = typeof( Disposable ).GetConstructors()[0];

        private int _disposed;
        private Action Disposer { get; } = dispose;

        public void Dispose()
        {
            if ( Interlocked.CompareExchange( ref _disposed, 1, 0 ) == 0 )
                Disposer.Invoke();
        }
    }
}
