﻿using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;
using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public CallStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( function, middleware, configure )
    {
    }

    // public FunctionAsync<TInput, TOutput> Bind( ProcedureAsync<TOutput> next, MethodInfo method = null )
    // {
    //     var defaultName = (method ?? next.Method).Name;
    //
    //     return async ( context, argument ) =>
    //     {
    //         var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
    //
    //         if ( canceled )
    //             return default;
    //
    //         return await ProcessStatementAsync(
    //             async ( ctx, arg ) =>
    //             {
    //                 await next( ctx, arg ).ConfigureAwait( false );
    //                 return arg;
    //             }, context, nextArgument, defaultName ).ConfigureAwait( false );
    //     };
    // }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<ProcedureAsync<TOutput>> next, MethodInfo method = null )
    {
        var defaultName = method?.Name ?? "defaultName";

        var context = Parameter( typeof( IPipelineContext ), "context");
        var argument = Parameter( typeof( TInput ), "argument" );

        var awaitedResult = Variable( typeof( (TOutput, bool) ), "awaitedResult"  );
        var nextArgument = Field( awaitedResult, "Item1" );
        var canceled = Field( awaitedResult, "Item2" );

        // inner function
        var ctx = Parameter( typeof( IPipelineContext ), "ctx" );
        var arg = Parameter( typeof( TInput ), "arg" );
        var nextExpression = Lambda<FunctionAsync<TOutput, TInput>>(
            BlockAsync(
                Await( Invoke( next, ctx, arg ), configureAwait: false ),
                argument
            ),
            parameters: [ctx, arg]
        );

        return Lambda<FunctionAsync<TInput, TOutput>>(
            BlockAsync(
                [awaitedResult],
                Assign( awaitedResult, Await( ProcessPipelineAsync( context, argument ), configureAwait: false ) ),
                Condition( canceled,
                    Default( typeof(TOutput) ),
                    // TODO: Think there is a bug here, we shouldn't need a child state machine.
                    // Await(
                    //     BlockAsync(
                    //         [awaitedResult],
                    Block( 
                        // [awaitedResult],
                            Await(
                                ProcessStatementAsync( nextExpression, context, nextArgument, defaultName ),
                                configureAwait: false
                            ),
                            nextArgument
                            )
                    //     )
                    // )
                )
            ),
            parameters: [context, argument]
        );
    }
}

