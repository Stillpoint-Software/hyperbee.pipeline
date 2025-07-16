---
layout: default
title: Extending Pipelines
nav_order: 6
---

# Extending Pipelines

The preferred way to extend pipeline functionality is by writing extension methods. This approach is simple, type-safe, and leverages C#'s strengths. Only create a new binder if you need to introduce a fundamentally new control flow or block structure that cannot be expressed with existing builders and extension methods.

## Extending with Extension Methods

Extension methods allow you to add new pipeline steps, middleware, or behaviors without modifying the core pipeline code. Place extension methods in well-named static classes (e.g., `PipelineExtensions`, `PipelineMiddleware`) for discoverability.

### Example: Adding a Custom Step

```csharp
public static class PipelineExtensions
{
    public static IPipelineBuilder<TStart, TOutput> WithCustomStep<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> builder)
    {
        return builder.Pipe((ctx, arg) => /* custom logic */);
    }
}
```

- Extension methods should be generic and type-safe where possible.
- Provide usage examples in the documentation.

## When to Create a Binder

Most customizations can be achieved with extension methods. However, if you need to introduce a new control flow (e.g., conditional, loop, parallel execution) or a new block structure, you may need to implement a custom binder.

### Example: Custom Binder for Random Skip

Suppose you want to add a pipeline step that randomly skips the next step in the pipeline, just for demonstration. This kind of execution control cannot be done with a simple extension method because it requires direct control over the pipeline's flow.

```csharp
public class RandomSkipBinder<TStart, TOutput> : Binder<TStart, TOutput>
{
    private readonly Random _random = new();

    public RandomSkipBinder(FunctionAsync<TStart, TOutput> function, Action<IPipelineContext> configure = null)
        : base(function, configure) { }

    public FunctionAsync<TStart, TOutput> Bind(FunctionAsync<TStart, TOutput> next)
    {
        return async (context, argument) =>
        {
            // 50% chance to skip the next step
            if (_random.NextDouble() < 0.5)
            {
                // Skip the next step and just return the current result
                return await Pipeline(context, argument);
            }
            // Otherwise, continue as normal
            return await next(context, argument);
        };
    }
}
```

You would then integrate this binder into your pipeline using a builder or extension method. This example is intentionally whimsical, but it demonstrates how a custom binder can control execution flow in ways that extension methods cannot.

## Best Practices

- Prefer extension methods for most customizations.
- Use binders only for advanced or structural changes to pipeline flow.
- Keep extension methods and binders well-documented and tested.

---

For more information, see the [conventions](conventions.md) and [middleware](middleware.md) documentation.
