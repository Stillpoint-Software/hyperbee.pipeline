---
layout: default
title: Conventions
nav_order: 3
---

# Conventions

This document describes the conventions for creating builders, binders, and middleware in the Hyperbee Pipeline library. Adhering to these conventions ensures consistency, maintainability, and clarity across the codebase and for all contributors.

## Generic Type Parameter Naming

- **TStart**: The initial input type to the pipeline. This type remains constant throughout the pipeline's lifetime.
- **TOutput**: The output type of the current builder or binder. This is the type produced by the current step and consumed by the next.
- **TNext**: The output type of the next function in the pipeline chain.

### Example

```csharp
public class PipelineBuilder<TStart, TOutput> { /* ... */ }
public interface IPipelineBuilder<TStart, TOutput> { /* ... */ }
```

## Builder and Binder Patterns

- Builders and binders should be designed to maximize composability and type safety.
- Prefer strongly-typed generics over `object` wherever possible.
- Use clear, descriptive names for builder and binder classes to indicate their role in the pipeline.
- Document the expected input and output types in XML comments.

## Middleware Conventions

### Hook Middleware

Hook middleware is always generic and type-safe. It is inserted at known points in the pipeline where the input and output types are known. Always use generic signatures for hook middleware:

```csharp
public delegate Task<TOutput> MiddlewareAsync<TStart, TOutput>(IPipelineContext context, TStart argument, FunctionAsync<TStart, TOutput> next);
```

### Wrap Middleware

Wrap middleware must be able to wrap any pipeline segment, regardless of its input and output types. To enable this, wrap middleware uses `object` for its input and output types. This is a necessary compromise in C# to allow full compositionality:

```csharp
public delegate Task<object> MiddlewareAsync<object, object>(IPipelineContext context, object argument, FunctionAsync<object, object> next);
```

When implementing wrap middleware:

- Use `object` for input and output types.
- Document the expected types and perform runtime checks and casts as needed.
- Only use this pattern for middleware that must be able to wrap arbitrary pipeline segments.

This distinction allows hook middleware to remain type-safe, while enabling wrap middleware to provide maximum flexibility.

- Middleware should be implemented using the `MiddlewareAsync<TStart, TOutput>` delegate:
  ```csharp
  public delegate Task<TOutput> MiddlewareAsync<TStart, TOutput>(IPipelineContext context, TStart argument, FunctionAsync<TStart, TOutput> next);
  ```

## Extending the Pipeline

- When creating custom builders or binders, follow the established naming and type parameter conventions.
- Register new pipeline steps using extension methods for discoverability.
- Provide XML documentation and usage examples for all public APIs.

## Documentation and Examples

- All new builders, binders, and middleware should be documented in the `docs/` directory.
- Include code samples and diagrams where appropriate.

---

For more information, see the [middleware documentation](middleware.md) and the [API reference](index.md).
