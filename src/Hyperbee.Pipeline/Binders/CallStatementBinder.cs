using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public CallStatementBinder( FunctionAsync<TInput, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, middleware, configure )
    {
    }

    public FunctionAsync<TInput, TOutput> Bind( ProcedureAsync<TOutput> next, MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessStatementAsync(
                async ( ctx, arg ) =>
                {
                    await next( ctx, arg ).ConfigureAwait( false );
                    return arg;
                }, context, nextArgument, defaultName ).ConfigureAwait( false );
        };
    }
}

