using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders;

internal class PipeStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public PipeStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( function, middleware, configure )
    {
    }

    // public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next, MethodInfo method = null )
    // {
    //     var defaultName = method?.Name ?? "name";
    //
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         return await ProcessStatementAsync( next, context, nextArgument, defaultName ).ConfigureAwait( false );
    //     };
    // }


    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next, MethodInfo method = null )
    {
        var defaultName = method?.Name ?? "defaultName";

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var body = BlockAsync(
            [awaitedResult],
            Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),
            Condition( canceled,
                Default( typeof(TNext) ),
                Await( ProcessStatementAsync( next, context, nextArgument, defaultName ), configureAwait: false )
            )
        );

        return Lambda<FunctionAsync<TInput, TNext>>(
            body,
            parameters: [context, argument]
        );

        //
        // var body = BlockAsync(
        //     [awaitedResult],
        //     Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),
        //     IfThen( canceled,
        //         Return( returnLabel, Default( typeof(TNext) ) )
        //     ),
        //     Label( returnLabel,
        //         Await( ProcessStatementAsync( next, context, nextArgument, defaultName ), configureAwait: false ) )
        // );
        //
        // return Lambda<FunctionAsync<TInput, TNext>>(
        //     body,
        //     parameters: [context, argument]
        // );
    }
}
