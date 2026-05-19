# ADR-0001: Pipelines Halt Step Progression on Error by Default

**Status:** Accepted
**Date:** 2026-05-19

## Context

A composed command - or any step whose inner `PipelineBuilder.Build()` catches an
exception - sets `context.Exception` / `context.IsError` on the shared pipeline
context, but the pipeline continues executing subsequent steps on `default` data. The
real-world failure (Ringba/Ringba-v2 PR #5597): an Aerospike deserialization error
inside a composed `getAccountByIdCommand` was swallowed, the next step received
`account == null`, and the pipeline reported a misleading, data-destroying "Account
not found." validation outcome. The original exception sat unread on the context.

An external proposal (`pipeline-error.md`) diagnosed this as `ExecuteAsync`-based
composition swallowing exceptions on an isolated inner `CommandResult.Context`, and
proposed rewriting all `CommandStatementBuilder` overloads to call `ExecuteAsync` and
re-throw via `ExceptionDispatchInfo`. Investigation showed that diagnosis is false for
this codebase:

- `CommandStatementBuilder` composes commands via `command.PipelineFunction` bound
  into the parent pipeline with the shared context. It never calls `ExecuteAsync`.
  There is no isolated inner context.
- `docs/site/command-pattern.md` explicitly documents shared-context composition as
  the intended design ("context flows through naturally ... shared state, middleware,
  exception handling, and cancellation are all preserved").
- Existing passing tests (`CommandStatementBuilderTests`) pin shared-context behavior;
  the proposed `ExecuteAsync` rewrite would break them, contradicting its "purely
  additive" claim.

Two forces were in tension:

1. Boundary model. The outermost pipeline returns a result and does not throw -
   directly analogous to Roslyn's `CSharpCompilation.Emit()` returning an `EmitResult`
   with `Diagnostics` rather than throwing on a compile error. `CommandResult
   { Context }` with `Success` / `IsError` / `Exception` is the diagnostics analog.
   This boundary model is considered correct and must be preserved.

2. Internal progression. Roslyn does not run the emit phase on garbage after binding
   fails; it halts phase progression and surfaces diagnostics at the boundary. This
   pipeline halts internal progression on cancellation (`Binder.ProcessPipelineAsync`
   then `HandleCancellationRequested`, with every binder bailing via
   `if (canceled) return default;`) but has no equivalent for error.

The validation subsystem already establishes the house pattern for halting on a
logical failure: `context.CancelAfter()` plus diagnostic-as-data in `context.Items`
plus result at the boundary (`PipelineValidationExtensions`,
`ValidationAction.CancelAfter`). Exceptions caught by `Build()` are the only failure
class that does not enter this established halt path.

Alternatives considered:

- Original proposal (ExecuteAsync rewrite + rethrow). Rejected: fights the correct
  boundary model, breaks documented shared-context design and existing tests, and
  patches one builder for a defect that lives in the shared binder/build layer.
- Strategy A - parallel `errored` flag through `ProcessPipelineAsync`. Add an error
  short-circuit alongside the cancellation one, touching the base method plus ~8
  binder call sites. Rejected: non-minimal, adds a second halt mechanism, no added
  correctness over Strategy B.
- Strategy B - reuse the existing cancel-after halt. Chosen (see Decision).

## Decision

We will make pipelines halt step progression on error by default, reusing the existing
cancellation short-circuit rather than introducing a new mechanism.

- In `PipelineBuilder.Build()` and `BuildAsProcedure()` catch blocks, when the
  halt-on-error policy is enabled, call `context.CancelAfter()` in addition to setting
  `context.Exception = ex`. The existing `HandleCancellationRequested` short-circuit
  in `Binder.ProcessPipelineAsync` then halts all downstream steps. No binder changes.
  All 8 step/block binders
  (Pipe / Call / PipeIf / CallIf / ForEach / Reduce / WaitAll / CallBlock) already
  bail via the existing `if (canceled)` pattern off the single
  `Binder.ProcessPipelineAsync` chokepoint. `WrapBinder` / `HookBinder` are middleware
  decorators and are out of scope. `WaitAll` forks child contexts via `Clone(false)`;
  parallel join semantics remain owned by its reducer (a known, documented boundary).

- The outermost boundary model is unchanged: `Build()` still returns a result and does
  not throw; `context.Exception` / `IsError` carry the diagnostic. This preserves the
  Roslyn-style result-plus-diagnostics contract.

- The behavior is governed by a halt-on-error policy carried on the context as a peer
  of `Throws`, seeded by `PipelineContextFactory` from a new public `PipelineOptions`
  object configured via an `Action<PipelineOptions> configure` delegate on
  `AddPipeline(...)` at DI wire-up. `PipelineOptions` is the single extensible seam for
  future pipeline-wide policy (chosen over a bare `bool` parameter to avoid
  option-parameter accretion). The default is halt-on-error = true (greenfield, IJW,
  Roslyn-correct); omitting the `configure` delegate keeps it on. A manually
  constructed `new PipelineContext()` also defaults to true. No per-call override is
  introduced in v1 (the signature can be extended later without a breaking change if a
  real need emerges).

Backward-compatibility contract: After this change, the new halt-on-error behavior is
the default. An existing user can restore the prior run-through behavior by explicitly
configuring the halt-on-error option to false at pipeline DI wire-up. No code changes
are required to adopt the new default.

## Consequences

Easier:

- `.PipeAsync(command)` and every other step behave like an ordinary `await` from the
  caller's perspective: an error stops the pipeline instead of silently corrupting
  downstream data. The Ringba `context.ThrowIfError()` workaround becomes unnecessary.
- One halt mechanism for all logical failures (validation, cancellation, error),
  consistent with the existing validation pattern. Minimal change surface; zero binder
  edits; low regression risk.

Harder / tradeoffs accepted:

- An errored pipeline now also reports `IsCanceled == true` (halt uses `CancelAfter`).
  This is consistent with existing behavior - validation failures already set
  `IsCanceled` today. `Success == !IsError && !IsCanceled` is unaffected, and
  `IsError` continues to distinguish exception failures. This overlap must be
  documented explicitly.
- This is a behavioral change for any existing pipeline that intentionally relies on
  running steps after an error was stashed (for example inline compensation that
  inspects or clears `context.Exception`). Such users must set the opt-out option at
  wire-up. The change is gated precisely so this remains a one-line, code-free
  migration.
- Parallel `WaitAll` branches run on forked (`Clone(false)`) contexts; halt-on-error
  applies per fork, and join/aggregation semantics remain the reducer's
  responsibility. This boundary is unchanged but must be documented.

Follow-on / constrained:

- Supersedes the `pipeline-error.md` proposal entirely; a corrected proposal documents
  the implementation (`docs/proposals/0001-halt-on-error.md`).
- A future per-call or per-builder override (for example opt a single composed command
  out of halt-on-error) can be added later as a non-breaking signature extension if
  demand is demonstrated. Deliberately out of scope now.
- `docs/site/command-pattern.md` must be revised: the "exception handling ...
  preserved" line now means "errors halt the pipeline and surface at the boundary,"
  and a Halt-on-Error / boundary-model section must be added.
