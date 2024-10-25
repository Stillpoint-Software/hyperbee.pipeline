using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class PipeBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public PipeBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    // public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         return await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
    //     };
    // }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( Expression<FunctionAsync<TOutput, TNext>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TNext ), "argument" );

        var awaitedResult = Variable( typeof( (TNext, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        return Lambda<FunctionAsync<TInput, TNext>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ) ) ),
                Condition( canceled,
                    Default( typeof( TNext ) ),
                    Await( ProcessBlockAsync( next, context, nextArgument ), configureAwait: false )
                )
            ),
            parameters: [context, argument]
        );
    }

}







public static class LoggerExpression
{
    public static Expression<Action<object>> Log( string message )
    {
        return arg1 => Log( message, arg1 );
    }

    public static void Log( string message, object arg1 )
    {
        Console.WriteLine( $"{message} value: {arg1}" );
    }
}
