using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput>
{
    private FunctionAsync<TInput, TOutput> Pipeline { get; }
    private MiddlewareAsync<object, object> Middleware { get; }
    private Action<IPipelineContext> Configure { get; }

    public CallStatementBinder( FunctionAsync<TInput, TOutput> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
    {
        Pipeline = function;
        Middleware = middleware;
        Configure = configure;
    }

    public FunctionAsync<TInput, TOutput> Bind( ProcedureAsync<TOutput> next, MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        return async ( context, argument ) =>
        {
            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );

            var contextControl = (IPipelineContextControl) context;

            if ( contextControl.HandleCancellationRequested( nextArgument ) )
                return default;

            using ( contextControl.CreateFrame( context, Configure, defaultName ) )
            {
                return await Next( next, context, nextArgument ).ConfigureAwait( false );
            }
        };
    }

    private async Task<TOutput> Next( ProcedureAsync<TOutput> next, IPipelineContext context, TOutput nextArgument )
    {
        if ( Middleware == null )
        {
            await next( context, nextArgument ).ConfigureAwait( false );
            return nextArgument;
        }

        await Middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) =>
            {
                await next( context1, (TOutput) argument1 ).ConfigureAwait( false );
                return nextArgument;
            }
        ).ConfigureAwait( false );

        return nextArgument;
    }
}