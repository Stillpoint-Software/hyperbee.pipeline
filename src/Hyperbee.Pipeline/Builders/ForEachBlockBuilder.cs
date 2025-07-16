using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class ForEachBlockBuilder
{
    public static ForEachBlockBuilderWrapper<TStart, TOutput> ForEach<TStart, TOutput>( this IPipelineBuilder<TStart, TOutput> parent )
    {
        return new ForEachBlockBuilderWrapper<TStart, TOutput>( parent );
    }

    public class ForEachBlockBuilderWrapper<TStart, TOutput>( IPipelineBuilder<TStart, TOutput> parent )
    {
        public IPipelineBuilder<TStart, TOutput> Type<TElement>(Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder)
        {
            return Type<TElement>(true, builder);
        }

        public IPipelineBuilder<TStart, TOutput> Type<TElement>(bool inheritMiddleware, Func<IPipelineStartBuilder<TElement, TElement>, IPipelineBuilder> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();
            var block = PipelineFactory.Start<TElement>(inheritMiddleware ? parentMiddleware : null);
            var result = builder(block);
            var function = result.CastFunction<TElement, object>();
            var middleware = (result as IPipelineFunctionProvider<TElement, object>)?.GetPipelineFunction().Middleware;

            return new PipelineBuilder<TStart, TOutput>
            {
                Function = new ForEachBlockBinder<TStart, TOutput, TElement>(parentFunction).Bind(function),
                Middleware = parentMiddleware
            };
        }
    }
}



