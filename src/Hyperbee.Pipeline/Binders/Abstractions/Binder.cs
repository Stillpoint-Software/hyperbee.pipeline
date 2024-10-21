using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;

using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;


namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class Binder<TInput, TOutput>
{
    protected Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    protected Expression<Action<IPipelineContext>> Configure { get; }

    protected Binder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<Action<IPipelineContext>> configure )
    {
        Pipeline = function;
        Configure = configure;
    }

    // protected virtual Task<(TOutput Result, bool Canceled)> ProcessPipelineAsync( IPipelineContext context, TInput argument )
    // {
    //     var result = await Pipeline( context, argument ).ConfigureAwait( false );
    //     
    //     var contextControl = (IPipelineContextControl) context;
    //     var canceled = contextControl.HandleCancellationRequested( result );
    //     
    //     return (canceled ? default : result, canceled);
    // }

    protected virtual Expression ProcessPipelineAsync( ParameterExpression context, ParameterExpression argument ) 
    {
        var tupleCtor = typeof(ValueTuple<TOutput, bool>).GetConstructor( [typeof(TOutput), typeof(bool)] )!;

        var resultVariable = Variable( typeof( TOutput ), "result" );
        var canceledVariable = Variable( typeof( bool ), "canceled" );

        var contextControl = Convert( context , typeof( IPipelineContextControl ) );

        var body = BlockAsync(
            [resultVariable, canceledVariable],
            Assign( resultVariable, Await( Invoke( Pipeline, context,  argument ), configureAwait: false ) ),
            Assign( canceledVariable, HandleCancellationRequested( contextControl, resultVariable ) ),

            Condition(
                canceledVariable,
                New( tupleCtor, Default( typeof( TOutput ) ), canceledVariable ),
                New( tupleCtor, resultVariable, canceledVariable )
            )
        );

        return body;
    }



    /*
    public static bool HandleCancellationRequested<TOutput>( this IPipelineContextControl control, TOutput value )
       {
           if ( !control.CancellationToken.IsCancellationRequested )
               return false;

           if ( !control.HasCancellationValue )
               control.CancellationValue = value;

           return true;
       }
     */


    private Expression HandleCancellationRequested( Expression contextControl, Expression resultVariable )
    {
        var hasCancellationValue = Property( contextControl, "HasCancellationValue" );
        var cancellationTokenProperty = Property( contextControl, "CancellationToken" );
        var cancellationValueProperty = Property( contextControl, "CancellationValue" );

        var conditionalExpression = Condition(
            Not( Property( cancellationTokenProperty, "IsCancellationRequested" ) ),
            Constant( false ),

            Block(
                IfThen(
                    Not( hasCancellationValue ),
                    Assign( cancellationValueProperty, resultVariable )
                ),
                // After the assignment, return true
                Constant( true )
            )
        );

        return conditionalExpression;
    }

}
