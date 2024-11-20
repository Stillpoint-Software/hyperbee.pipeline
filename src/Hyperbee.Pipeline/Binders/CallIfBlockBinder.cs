﻿using System.Linq.Expressions;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class CallIfBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public CallIfBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TOutput, object>> next )
    {
        /*    
        {
            return async ( context, argument ) =>
            {
                var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    
                if ( canceled )
                    return default;
    
                await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
                return nextArgument;
            };
        }
        */

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult" );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        var returnLabel = Label( "return" );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( Invoke( ProcessPipelineAsync( context, argument ) ) ) ),
                IfThen( canceled,
                    Return( returnLabel, Default( typeof( TOutput ) ) )
                ),
                Await( Invoke( ProcessBlockAsync( next, context, nextArgument ) ), configureAwait: false ),
                Label( returnLabel, nextArgument )
            ),
            parameters: [context, argument]
        );
    }
}
