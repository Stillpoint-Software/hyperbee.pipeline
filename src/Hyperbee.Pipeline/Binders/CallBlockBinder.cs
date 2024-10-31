using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TInput, TOutput> : BlockBinder<TInput, TOutput>
{
    public CallBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    // public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TOutput, object> next )
    // {
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
    //         return nextArgument;
    //     };
    // }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TOutput, object>> next )
    {
        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var result = Variable( typeof( TOutput ), "result" );

        var returnValue = Label( typeof( TOutput ) );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult, result],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ), configureAwait: false ) ),

                IfThenElse( canceled,
                    Block(
                        Assign( result, Default( typeof( TOutput ) ) )//,
                                                                      //Return( returnValue, result )
                    ),
                    Block(
                        Await(
                            ProcessBlockAsync( next, context, nextArgument ),
                            configureAwait: false
                        ),
                        Assign( result, nextArgument )//,
                                                      //Return( returnValue, result )
                    )
                ),
                result
            //Label( returnValue, result )
            ),
            parameters: [context, argument]
        );
    }
}
