//using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

//internal class ForEachBlockBinder<TInput, TOutput, TElement>
//{
//    private FunctionAsync<TInput, TOutput> Pipeline { get; }

//    public ForEachBlockBinder( FunctionAsync<TInput, TOutput> function )
//    {
//        Pipeline = function;
//    }

//    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TElement, object> next )
//    {
//        return async ( context, argument ) =>
//        {
//            var nextArgument = await Pipeline( context, argument ).ConfigureAwait( false );
//            var nextArguments = (IEnumerable<TElement>) nextArgument;

//            foreach ( var elementArgument in nextArguments )
//            {
//                await next( context, elementArgument ).ConfigureAwait( false );
//            }

//            return nextArgument;
//        };
//    }
//}

    internal class ForEachBlockBinder<TInput, TOutput, TElement> : BlockBinder<TInput, TOutput>
    {
        public ForEachBlockBinder( FunctionAsync<TInput, TOutput> function )
            : base( function, default )
        {
        }

        public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TElement, object> next )

        {
            return async ( context, argument ) =>
            {
                var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument ).ConfigureAwait( false );
                
                if ( canceled ) 
                    return default;

                var nextArguments = (IEnumerable<TElement>) nextArgument;

                foreach ( var elementArgument in nextArguments )
                {
                    await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
                }

                return nextArgument;
            };
        }
    }

