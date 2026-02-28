## Summary

- Add first-class support for composing command pipelines directly into other pipelines via `PipeAsync`, `CallAsync`, `PipeIf`, and `CallIf` overloads that accept command types
- Add implicit cast operators on `CommandFunction` and `CommandProcedure` for general interoperability
- Expose `PipelineFunction` property on command interfaces to enable pipeline extraction without `ExecuteAsync`

## Motivation

When one command's pipeline needs to call another command's pipeline, the only current path is through `ExecuteAsync`:

```csharp
// Current (ugly) approach inside CreatePipeline():
.PipeAsync(async (ctx, arg) => {
    var result = await _otherCommand.ExecuteAsync(arg, ctx.CancellationToken);
    return result.Result;
})
```

This has three problems:

1. **Breaks the context chain** — `ExecuteAsync` creates a fresh `PipelineContext` via `ContextFactory.Create()`, so `Items`, exception state, and middleware from the calling pipeline are lost.
2. **Boilerplate** — every call site wraps in a lambda, unpacks `CommandResult`, and manually threads the cancellation token.
3. **Semantic mismatch** — the pipeline delegate (`FunctionAsync<TStart, TOutput>`) is already inside the command (`Pipeline.Value`), ready to compose directly.

## Design

### How pipeline-to-pipeline already works

A built pipeline is just a `FunctionAsync<TStart, TOutput>` delegate. Composing them is already clean:

```csharp
var childPipeline = PipelineFactory.Start<string>().Pipe(...).Build();

var parentPipeline = PipelineFactory.Start<string>()
    .PipeAsync(childPipeline)  // direct composition, context flows through
    .Build();
```

The problem is that commands **hide** their pipeline delegate behind `protected Lazy<FunctionAsync<TStart, TOutput>> Pipeline`.

### Solution: Two layers

**Layer 1 — `PipelineFunction` property on interfaces + implicit operators on concrete classes:**

```csharp
// Interface exposes the pipeline delegate
public interface ICommandFunction<in TStart, TOutput> : ICommand
{
    FunctionAsync<TStart, TOutput> PipelineFunction { get; }
    // ... existing ExecuteAsync methods
}

// Concrete class adds implicit cast
public static implicit operator FunctionAsync<TStart, TOutput>(
    CommandFunction<TStart, TOutput> command) => command.Pipeline.Value;
```

**Layer 2 — Builder extension overloads:**

New overloads of `PipeAsync`, `CallAsync`, `PipeIf`, and `CallIf` that accept command types directly. These use the existing binder infrastructure — the command's `PipelineFunction` is passed straight through as the `FunctionAsync` delegate the binders already expect.

```csharp
// Usage — clean, context flows through naturally
protected override FunctionAsync<string, int> CreatePipeline()
{
    return PipelineFactory
        .Start<string>()
        .Pipe(DoSomething)
        .PipeAsync(_commandB)       // ICommandFunction overload
        .CallAsync(_commandC)       // ICommandProcedure overload
        .PipeIf(SomeCondition, _commandD)
        .Build();
}
```

### Why expose PipelineFunction on the interface?

Commands are resolved from DI via their interfaces (`ICommandFunction<TStart, TOutput>` or specific interfaces like `IGetUserCommand`). C# doesn't allow implicit operators on interfaces, and generic type inference doesn't consider user-defined implicit conversions. So the implicit operator alone isn't sufficient for the DI case — `PipelineFunction` bridges that gap.

`ExecuteAsync` remains available for when a fresh context boundary is truly desired (e.g., calling from an API controller).

## Test plan

- [ ] `PipeAsync` with `ICommandFunction<TOutput, TNext>` composes correctly and shares context
- [ ] `CallAsync` with `ICommandProcedure<TOutput>` runs for side effects and preserves input value
- [ ] `PipeIf` with command only runs the command when condition is true
- [ ] `CallIf` with command only runs the command when condition is true
- [ ] Implicit cast from `CommandFunction` to `FunctionAsync` works
- [ ] Implicit cast from `CommandProcedure` to `ProcedureAsync` works
- [ ] Documentation updated
