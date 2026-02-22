---
layout: default
title: Conventions
nav_order: 3
---

# Conventions

This document describes the conventions for creating builders, binders, and middleware in the Hyperbee Pipeline library. Adhering to these conventions ensures consistency, maintainability, and clarity across the codebase and for all contributors.

## Monadic Foundations

The Hyperbee Pipeline is monadic. With `TStart` fixed, `PipelineBuilder<TStart, _>` forms a monad over the second type parameter. Each pipeline step is a [Kleisli arrow](https://en.wikipedia.org/wiki/Kleisli_category) (`A -> Task<B>`), and `Pipe` is Kleisli composition.

| Monad Operation | Pipeline Equivalent | Description |
|-----------------|---------------------|-------------|
| **return** / **pure** | `PipelineFactory.Start<TStart>()` | Wraps the identity function into the pipeline |
| **bind** / **>>=** | `Binder.Bind<TNext>(FunctionAsync<TOutput, TNext>)` | Composes a new step, producing `FunctionAsync<TStart, TNext>` |

This is analogous to how `Reader<R, _>` or `State<S, _>` monads fix their first parameter and operate monadically on the second.

## Generic Type Parameter Naming

### Pipeline-Level Parameters

| Parameter | Role | Where Used |
|-----------|------|------------|
| `TStart` | Invariant initial input type | `PipelineBuilder<TStart, TOutput>`, all binders and builders |
| `TOutput` | Output type of the current step | Builder/binder class parameters |
| `TNext` | Output type of the next step being composed | `Bind<TNext>()` method parameters |
| `TElement` | Element type within a collection | `ForEachBlockBinder`, `ReduceBlockBinder` |
| `TArgument` | Flexible input type for block operations | `BlockBinder.ProcessBlockAsync<TArgument, TNext>` |

### TStart in Delegates

The delegate definitions also use `TStart` as their first type parameter:

```csharp
public delegate Task<TOutput> FunctionAsync<in TStart, TOutput>(...);
```

At first glance, this seems inconsistent because these delegates are instantiated with types other than the pipeline's `TStart`:

```csharp
FunctionAsync<TOutput, TNext> next       // step function
FunctionAsync<TElement, TNext> next       // reduce step
Function<TOutput, bool> condition         // condition predicate
```

**This is intentional and correct.** From a Kleisli perspective, every function is itself a composable pipeline. When a step function is typed as `FunctionAsync<TOutput, TNext>`, `TOutput` is that step's own "start." The naming is self-similar: `TStart` means "the start of *this* composition" at every level of abstraction -- whether "this" is the entire pipeline or a single step.

### Type Flow Through Composition

The following diagram shows how generic type parameters flow when composing pipeline steps:

```
PipelineFactory.Start<string>()        -> IPipelineStartBuilder<string, string>
                                           TStart=string, TOutput=string

.Pipe((ctx, arg) => int.Parse(arg))    -> IPipelineBuilder<string, int>
                                           TStart=string, TOutput=int
                                           step function: FunctionAsync<string, int>
                                                          (step's own TStart=string)

.Pipe((ctx, arg) => arg * 2)           -> IPipelineBuilder<string, int>
                                           TStart=string, TOutput=int
                                           step function: FunctionAsync<int, int>
                                                          (step's own TStart=int)

.Pipe((ctx, arg) => arg.ToString())    -> IPipelineBuilder<string, string>
                                           TStart=string, TOutput=string
                                           step function: FunctionAsync<int, string>
                                                          (step's own TStart=int)

.Build()                               -> FunctionAsync<string, string>
```

Notice that `TStart` (the pipeline's invariant start type) remains `string` throughout, while `TOutput` evolves with each step. Each step function has its own `TStart` which is the previous step's `TOutput`.

### When to Use TElement vs TArgument

- **TElement**: Use when operating on individual elements of a collection that flows through the pipeline. Used by `ForEachBlockBinder` and `ReduceBlockBinder` where `TOutput` is `IEnumerable<TElement>`.
- **TArgument**: Use in `BlockBinder.ProcessBlockAsync<TArgument, TNext>` when the next function's input type may differ from `TOutput` (e.g., in Reduce and ForEach operations where the block processes elements, not the whole collection).

## Builder and Binder Patterns

Builder methods are forward-looking: they always build the "next" step.

```
CurrentBuilder<TStart, TOutput>  ->  NextBuilder<TStart, TNext>
                                     via Binder.Bind<TNext>(FunctionAsync<TOutput, TNext>)
```

- Builders and binders should be designed to maximize composability and type safety.
- Prefer strongly-typed generics over `object` wherever possible.
- Use clear, descriptive names for builder and binder classes to indicate their role in the pipeline.
- Document the expected input and output types in XML comments.

### The Binder Hierarchy

```
Binder<TStart, TOutput>                  Base class: holds Pipeline function, ProcessPipelineAsync
|-- StatementBinder<TStart, TOutput>     Adds middleware support, ProcessStatementAsync
|   |-- PipeStatementBinder              Bind<TNext>: transforms TOutput -> TNext
|   +-- CallStatementBinder              Bind: side-effect, preserves TOutput
+-- BlockBinder<TStart, TOutput>         ProcessBlockAsync<TArgument, TNext>
    |-- PipeBlockBinder                  Bind<TNext>: nested pipeline, TOutput -> TNext
    |-- CallBlockBinder                  Bind: nested pipeline, preserves TOutput
    |-- ForEachBlockBinder<.., TElement> Bind: iterates TElement, preserves TOutput
    |-- ReduceBlockBinder<.., TElement, TNext>  Bind: reduces TElement -> TNext
    |-- WaitAllBlockBinder               Bind<TNext>: parallel execution with reducer
    +-- ConditionalBlockBinder           Adds condition evaluation
        |-- PipeIfBlockBinder            Bind<TNext>: conditional transform
        +-- CallIfBlockBinder            Bind: conditional side-effect
```

## Middleware Conventions

### Hook Middleware

Hook middleware is always generic and type-safe. It is inserted at known points in the pipeline where the input and output types are known. Hooks surround **individual** pipeline steps. Always use generic signatures for hook middleware:

```csharp
public delegate Task<TOutput> MiddlewareAsync<TStart, TOutput>(
    IPipelineContext context,
    TStart argument,
    FunctionAsync<TStart, TOutput> next);
```

Hooks must be constrained to the start of the pipeline by extending `IPipelineStartBuilder<TStart, TOutput>`.

### Wrap Middleware

Wrap middleware surrounds a **group** of pipeline steps. It must be able to wrap any pipeline segment regardless of its input and output types. To enable this, wrap middleware uses `object` for its input and output types. This is a necessary compromise in C# to allow full compositionality:

```csharp
MiddlewareAsync<object, object>
```

When implementing wrap middleware:

- Use `object` for input and output types.
- Document the expected types and perform runtime checks and casts as needed.
- Only use this pattern for middleware that must be able to wrap arbitrary pipeline segments.

This distinction allows hook middleware to remain type-safe, while enabling wrap middleware to provide maximum flexibility.

## Extending the Pipeline

- When creating custom builders or binders, follow the established naming and type parameter conventions.
- Register new pipeline steps using extension methods for discoverability.
- Provide XML documentation and usage examples for all public APIs.
- Most customizations can be achieved with extension methods. Only create a new binder for fundamentally new control flow or block structures. See [Extending Pipelines](extending.md).

## Documentation and Examples

- All new builders, binders, and middleware should be documented in the `docs/` directory.
- Include code samples and diagrams where appropriate.

---

For more information, see the [middleware documentation](middleware.md) and the [API reference](index.md).
