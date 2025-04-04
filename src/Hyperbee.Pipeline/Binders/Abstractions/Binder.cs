using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class Binder<TInput, TOutput>
{
    protected Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    protected Expression<Action<IPipelineContext>> Configure { get; }

    private static ConstructorInfo _tupleConstructor = typeof( ValueTuple<TOutput, bool> ).GetConstructor( [typeof( TOutput ), typeof( bool )] )!;

    protected Binder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<Action<IPipelineContext>> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    protected virtual Expression ProcessPipelineAsync( ParameterExpression context, ParameterExpression argument )
    {
        /*
        {
            var result = await Pipeline( context, argument ).ConfigureAwait( false );

            var contextControl = (IPipelineContextControl) context;
            var canceled = contextControl.HandleCancellationRequested( result );

            return (canceled ? default : result, canceled);
        }
        */

        var result = Variable( typeof( TOutput ), "result" );
        var canceled = Variable( typeof( bool ), "canceled" );

        var contextControl = Convert( context, typeof( IPipelineContextControl ) );

        return BlockAsync(
            [result, canceled],
            Assign( result, Await( Invoke( Pipeline, context, argument ), configureAwait: false ) ),
            Assign( canceled, HandleCancellationRequested( contextControl, result ) ),

            Condition(
                canceled,
                New( _tupleConstructor, Default( typeof( TOutput ) ), canceled ),
                New( _tupleConstructor, result, canceled )
            )
        );
    }

    private Expression HandleCancellationRequested( Expression contextControl, Expression resultVariable )
    {
        /*
        {
            if ( !control.CancellationToken.IsCancellationRequested )
                return false;

            if ( !control.HasCancellationValue )
                control.CancellationValue = value;

            return true;
        }
        */
        var hasCancellationValue = Property( contextControl, "HasCancellationValue" );
        var cancellationTokenProperty = Property( contextControl, "CancellationToken" );
        var cancellationValueProperty = Property( contextControl, "CancellationValue" );

        return Condition(
            Not( Property( cancellationTokenProperty, "IsCancellationRequested" ) ),
            Constant( false ),

            Block(
                IfThen(
                    Not( hasCancellationValue ),
                    Assign( cancellationValueProperty, Convert( resultVariable, typeof( object ) ) )
                ),
                // After the assignment, return true
                Constant( true )
            )
        );
    }

}
