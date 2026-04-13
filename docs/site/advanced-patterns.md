---
layout: default
title: Advanced Patterns
nav_order: 7
---

# Advanced Pipeline Patterns

This page demonstrates advanced usage of the Hyperbee Pipeline library, combining extension methods, custom binders, and middleware for powerful and flexible pipeline composition.

## Combining Custom Middleware, Extension Methods, and a Binder

Suppose you want to:

- Add logging to every step (middleware)
- Add a custom step via an extension method
- Use a custom binder to control flow (e.g., retry on failure)

### Custom Logging Middleware

```csharp
public static class PipelineMiddleware
{
    public static IPipelineStartBuilder<TStart, TOutput> WithLogging<TStart, TOutput>(this IPipelineStartBuilder<TStart, TOutput> builder)
    {
        return builder.HookAsync(async (ctx, arg, next) =>
        {
            Console.WriteLine($"[LOG] Before: {arg}");
            var result = await next(ctx, arg);
            Console.WriteLine($"[LOG] After: {result}");
            return result;
        });
    }
}
```

### Custom Step via Extension Method

```csharp
public static class PipelineExtensions
{
    public static IPipelineBuilder<TStart, TOutput> WithCustomTransform<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> builder, Func<TOutput, TOutput> transform)
    {
        return builder.Pipe((ctx, arg) => transform(arg));
    }
}
```

### Custom Retry Binder

```csharp
public class RetryBinder<TStart, TOutput> : Binder<TStart, TOutput>
{
    private readonly int _maxRetries;
    public RetryBinder(FunctionAsync<TStart, TOutput> function, int maxRetries = 3)
        : base(function, null) => _maxRetries = maxRetries;

    public FunctionAsync<TStart, TOutput> Bind(FunctionAsync<TStart, TOutput> next)
    {
        return async (context, argument) =>
        {
            int attempt = 0;
            while (true)
            {
                try { return await next(context, argument); }
                catch when (++attempt < _maxRetries) { }
            }
        };
    }
}
```

### Usage Example

```csharp
var pipeline = PipelineFactory
    .Start<string>()
    .WithLogging()
    .Pipe((ctx, arg) => arg + " step1")
    .WithCustomTransform(s => s.ToUpper())
    .Pipe((ctx, arg) => arg + " step2")
    .Pipe((ctx, arg) => throw new Exception("fail"))
    .Pipe((ctx, arg) => new RetryBinder<string, string>(null, 3).Bind((c, a) => Task.FromResult(a)))
    .Build();

var result = await pipeline(new PipelineContext(), "input");
```

This example demonstrates how to combine middleware, extension methods, and a custom binder for advanced scenarios.

---

For more, see [Extending Pipelines](extending.md) and [Middleware](middleware.md).
