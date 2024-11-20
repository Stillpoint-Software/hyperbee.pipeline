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
        /*
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
        */

        var control = Convert( context, typeof( IPipelineContextControl ) );

        var idVariable = Variable( typeof( int ), "originalId" );
        var nameVariable = Variable( typeof( string ), "originalName" );

        var idProperty = Property( control, "Id" );
        var nameProperty = Property( control, "Name" );

        var exception = Variable( typeof( Exception ), "exception" );

        return Block(
            [idVariable, nameVariable],
            Assign( idVariable, idProperty ),
            Assign( nameVariable, nameProperty ),
            TryCatch(
                Block(
                    Assign( idProperty, Call( control, "GetNextId", Type.EmptyTypes ) ),
                    Assign( nameProperty, Constant( defaultName ) ),
                    config != null
                        ? Invoke( config, context )
                        : Empty(),
                    New( Disposable.ConstructorInfo,
                        Lambda<Action>(
                            Block(
                                Assign( idProperty, idVariable ),
                                Assign( nameProperty, Constant( "lambdaName" ) )
                            )
                        ) )
                ),
                Catch(
                    exception,
                    Block(
                        [exception],
                        Assign( idProperty, idVariable ),
                        Assign( nameProperty, nameVariable ),
                        Throw( exception, typeof( Disposable ) )
                    )
                )
            )
        );
    }

    /*
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
    */

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
