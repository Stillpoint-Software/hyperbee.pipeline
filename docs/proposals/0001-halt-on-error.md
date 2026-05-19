# Proposal 0001: Halt-on-Error for Pipelines

**Status:** Implemented (branch `feat/halt-on-error`)
**Date:** 2026-05-19
**Decision:** ADR-0001 (docs/decisions/0001-pipelines-halt-on-error-by-default.md)
**Supersedes:** `pipeline-error.md` (desktop draft) - rejected; see "Why the original
draft was wrong" below.

## Goal

When any pipeline step fails with an exception, stop running subsequent steps and
surface the failure at the pipeline boundary as a result (not a throw). Make this the
default; allow existing users to restore the old run-through behavior with one
wire-up option.

## Why the original draft was wrong

The desktop draft (`pipeline-error.md`) is rejected. Its premise does not match this
codebase:

- It claims `CommandStatementBuilder` invokes `command.ExecuteAsync(...)` and swallows
  exceptions on an isolated inner `CommandResult.Context`. False. Composition binds
  `command.PipelineFunction` into the parent with the shared context; `ExecuteAsync`
  is never called. The draft's own footnote ("the exception lands on the outer context
  too, via the shared parent") contradicts its own premise.
- Its "purely additive" claim is false: switching composition to `ExecuteAsync` would
  break documented shared-context behavior (`docs/site/command-pattern.md`) and
  existing passing tests (`CommandStatementBuilderTests`).
- It patches one builder for a defect that is library-wide and lives in the
  build/binder layer.
- Its sample code does not compile against the real binder structure, and its test
  fixtures use the wrong `CommandFunction` constructor signature.

## Correct diagnosis

The outermost pipeline correctly returns a result and does not throw - analogous to
Roslyn `CSharpCompilation.Emit()` returning `EmitResult` + `Diagnostics`. That
boundary model is correct and is NOT changing.

The actual defect: internal step progression halts on cancellation but not on error.

- `Binder.ProcessPipelineAsync` (`src/Hyperbee.Pipeline/Binders/Abstractions/Binder.cs`)
  is the single chokepoint every step/block binder funnels through. It calls
  `HandleCancellationRequested`; every binder bails via `if (canceled) return default;`.
- The validation subsystem already establishes the house pattern for halting on a
  logical failure: `context.CancelAfter()` + diagnostic-as-data + result at boundary
  (`PipelineValidationExtensions`, `ValidationAction.CancelAfter`).
- An exception caught by `PipelineBuilder.Build()` sets `context.Exception` but never
  enters that halt path. That single gap is the bug.

## Design (Strategy B - reuse the existing cancel-after halt)

When `Build()` / `BuildAsProcedure()` catches an exception and the halt-on-error policy
is enabled, also call `context.CancelAfter()`. The existing cancellation short-circuit
then halts every downstream step with zero binder changes. Boundary behavior is
unchanged: a result is returned, `context.Exception` / `IsError` carry the diagnostic.

### File changes (all in `src/Hyperbee.Pipeline/` unless noted)

1. `Context/IPipelineContext.cs` - add `bool HaltOnError { get; }` (peer of `Throws`).
2. `Context/PipelineContext.cs` - add `HaltOnError` as an `init` property defaulting to
   `true` (same pattern as the existing `Logger` / `ServiceProvider` init properties,
   so the factory sets it via object initializer and a manual `new PipelineContext()`
   gets the new default). The `(source, throws)` clone constructor copies
   `HaltOnError = source.HaltOnError` so forked `WaitAll` branches inherit it.
3. `PipelineBuilder.cs` - in both `Build()` and `BuildAsProcedure()` catch blocks:

   ```
   catch ( Exception ex )
   {
       context.Exception = ex;

       if ( context.HaltOnError && !context.IsCanceled )
           context.CancelAfter();          // enters the existing halt path

       if ( context.Throws )
           throw;
   }
   ```

   `OperationCanceledException` already arrives with the token canceled, so the
   `!context.IsCanceled` guard avoids a redundant `CancelAfter` and keeps cancellation
   semantics intact.
4. `Context/PipelineOptions.cs` (new) - public options object, the single extensible
   seam for pipeline-wide policy:

   ```
   public sealed class PipelineOptions
   {
       public bool HaltOnError { get; set; } = true;
   }
   ```

5. `Context/IPipelineContextFactory.cs` / `Context/PipelineContextFactory.cs` - the
   factory carries a resolved `PipelineOptions` (default instance when none supplied)
   and stamps `HaltOnError = options.HaltOnError` onto every `PipelineContext` it
   creates via the object initializer. `CreateFactory(...)` gains a `PipelineOptions`
   parameter; the existing single-instance behavior is preserved.
6. `Extensions/ServiceCollectionExtensions.cs` - add an
   `Action<PipelineOptions> configure = null` to both existing `AddPipeline` overloads
   (it composes with `includeAllServices` and the `implementationFactory` overload).
   The delegate mutates a default `PipelineOptions` (so omitting it keeps
   halt-on-error on); the result is passed to `CreateFactory`. Wire-up shapes:

   ```
   // greenfield - halt-on-error is the default, nothing to configure
   services.AddPipeline();

   // legacy opt-out - one explicit setting, no code changes elsewhere
   services.AddPipeline( o => o.HaltOnError = false );

   // composes with the factory overload
   services.AddPipeline(
       ( factorySvcs, root ) => { /* ... */ },
       o => o.HaltOnError = false );
   ```
7. `docs/site/command-pattern.md` - rewrite the "exception handling ... preserved"
   sentence; add a "Halt-on-Error and the Boundary Model" section explaining
   result-not-throw at the boundary, halt-between-steps internally, and the
   `AddPipeline( o => o.HaltOnError = false )` opt-out.
8. Changelog - note the new default and the one-line opt-out.

### Backward compatibility

After this change the new halt-on-error behavior is the default. An existing user
restores the prior run-through behavior by explicitly setting the halt-on-error option
to `false` at pipeline DI wire-up. No code changes are required to adopt the new
default. A manually constructed `new PipelineContext()` also defaults to halt-on-error.

## Tests

New tests, reusing `CommandStatementBuilderTests` conventions (MSTest, AAA, NSubstitute
for `IPipelineContextFactory` / `ILogger`). Target ~8-10 tests, not 32.

1. `step_throws_should_halt_pipeline_and_skip_subsequent_steps` - a `.Pipe` after a
   throwing step does not run; result is `default`; `context.IsError` true;
   `context.Exception` is the thrown instance.
2. `composed_command_throws_should_halt_outer_pipeline` - the Ringba shape: a composed
   command throws, the follow-up validation step never runs, no misleading "not found".
3. `halt_on_error_false_preserves_legacy_run_through` - with the opt-out, subsequent
   steps still run, result mirrors current behavior; `context.IsError` true.
4. `boundary_does_not_throw_on_error` - outermost `Build()` returns a result, does not
   throw, when `Throws` is false (default).
5. `boundary_throws_when_Throws_true` - existing `Throws` semantics unchanged.
6. `errored_pipeline_reports_IsError_and_IsCanceled` - pins the documented overlap;
   `Success` is false; `IsError` distinguishes from a plain cancellation.
7. `OperationCanceledException_is_not_double_canceled` - cancellation path unchanged;
   no redundant `CancelAfter`; `CancellationValue` behavior intact.
8. `procedure_pipeline_halts_on_error` - same via `BuildAsProcedure()`.
9. `WaitAll_branch_error_is_isolated_to_fork` - a throwing parallel branch halts its
   own fork; the reducer still receives per-branch results; pins the documented
   parallel boundary.
10. `manual_PipelineContext_defaults_to_halt_on_error` - non-DI construction gets the
    new default.

`dotnet test` green; `dotnet build -warnaserror` clean. Existing
`CommandStatementBuilderTests` (shared-context) must remain green unchanged.

## API surface note

`IPipelineContext` gains a `bool HaltOnError { get; }` member. `PipelineContext` is the
only concrete implementer in the repo, so this is source-compatible internally. It is,
however, a breaking change for any external code that implements `IPipelineContext`
directly (they must add the member). This is acceptable for a minor/feature release of
a library that controls its own versioning; it is called out here so the release notes
can flag it. `CreateFactory` gained an optional trailing `PipelineOptions` parameter
(appended last) so all existing positional call sites compile unchanged.

## Out of scope (deliberately)

- Per-call / per-builder halt override. Omitted in v1 to avoid additive bias; the
  context/option signature can be extended later without a breaking change.
- Changing the outermost boundary model (still returns a result, still does not throw).
- Auto-merging validation results between composed and parent contexts (unrelated;
  composition already shares the context).
- Any `CommandStatementBuilder` / `ExecuteAsync` signature change. None is needed -
  the fix is in the build/context layer.

## Acceptance criteria

- [x] `IPipelineContext.HaltOnError` exists; `PipelineContext` defaults it to `true`
      (init property) and propagates it through the clone constructor.
- [x] `Build()` and `BuildAsProcedure()` call `context.CancelAfter()` on caught
      exception when `HaltOnError` and not already canceled; `Throws` behavior
      unchanged.
- [x] No binder changes; all existing binder/cancellation tests remain green.
- [x] New public `PipelineOptions { HaltOnError = true }`; both `AddPipeline`
      overloads accept `Action<PipelineOptions> configure = null` and compose with the
      existing `includeAllServices` / `implementationFactory` overloads.
- [x] `AddPipeline( o => o.HaltOnError = false )` reproduces the prior run-through
      behavior with no other code change; omitting `configure` keeps halt-on-error on.
- [x] Existing `CommandStatementBuilderTests` pass unchanged.
- [x] New test matrix implemented and green (9 tests in `HaltOnErrorTests.cs`;
      consolidated the plain-step case into composed-command coverage since a single
      `Build()` already short-circuits via normal exception propagation - the
      composed-command path is the actual defect surface).
- [x] `docs/site/command-pattern.md` updated (ASCII only); `dependency-injection.md`
      gains a Pipeline Options section. No `CHANGELOG` file exists in the repo
      (versioning via nbgv / GitHub Releases) - release note deferred to the release
      process; see handoff note.
- [x] `dotnet build -warnaserror` clean (full solution, net10.0).
- [x] All 244 tests green across all 6 test projects (net10.0); existing
      `CommandStatementBuilderTests` unchanged and passing.
