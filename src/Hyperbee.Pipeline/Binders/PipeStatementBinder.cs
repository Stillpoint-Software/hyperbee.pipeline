using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class PipeStatementBinder<TStart, TOutput> : StatementBinder<TStart, TOutput>
{
    public PipeStatementBinder( FunctionAsync<TStart, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, middleware, configure )
    {
    }

    public FunctionAsync<TStart, TNext> Bind<TNext>( FunctionAsync<TOutput, TNext> next, MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessStatementAsync( next, context, nextArgument, defaultName ).ConfigureAwait( false );
        };
    }
}
