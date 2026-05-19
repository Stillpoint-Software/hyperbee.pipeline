---
layout: default
title: Commands
nav_order: 6
---

# Command Pattern

`ICommand*` interfaces and `Command*` base classes provide a lightweight pattern for constructing injectable commands built
around pipelines and middleware.

| Interface                              | Class                                 | Description                                         |
| -------------------------------------- | ------------------------------------- | --------------------------------------------------- |
| ICommandFunction&lt;TStart,TOutput&gt; | CommandFunction&lt;TStart,TOutput&gt; | A command that takes an input and returns an output |
| ICommandFunction&lt;TOutput&gt;        | CommandFunction&lt;TOutput&gt;        | A command that takes no input and returns an output |
| ICommandProcedure&lt;TStart&gt;        | CommandProcedure&lt;TStart&gt;        | A command that takes an input and returns void      |

## Example 1

Example of a command that takes an input and produces an output.

```csharp
public interface IMyCommand : ICommandFunction<Guid, String>
{
}

public class MyCommand : CommandFunction<Guid, String>, IMyCommand
{
    public MyCommand( ILogger<GetMessageCommand> logger )
        : base( logger)
    {
    }

    protected override FunctionAsync<Guid, String> PipelineFactory()
    {
        return PipelineFactory
            .Start<Guid>()
            .WithLogging()
            .Pipe( GetString )
            .Build();
    }

    private async Task<String> GetString( IPipelineContext context, Guid id )
    {
        return id.ToString();
    }
}

// usage
void usage( IMyCommand command )
{
    var result = await command.ExecuteAsync( Guid.Create() ); // takes a Guid, returns a string
}
```

## Example 2

Example of a command that takes no input and produces an output.

```csharp
public interface IMyCommand : ICommandFunction<String>
{
}

public class MyCommand : CommandFunction<String>, IMyCommand
{
    public MyCommand( ILogger<GetMessageCommand> logger )
        : base( logger)
    {
    }

    protected override FunctionAsync<Arg.Empty, String> CreatePipeline()
    {
        return PipelineFactory
            .Start<Arg.Empty>()
            .WithLogging()
            .PipeAsync( GetString )
            .Build();
    }

    private String GetString( IPipelineContext context, Arg.Empty _ )
    {
        return "Hello";
    }
}

// usage
void usage( IMyCommand command )
{
    var result = await command.ExecuteAsync(); // returns "Hello"
}
```

## Example 3

Example of a command that takes an input and produces no output.

```csharp
public interface IMyCommand : ICommandProcedure<String>
{
}

public class MyCommand : CommandProcedure<String>, IMyCommand
{
    public GetCommand( ILogger<MyCommand> logger )
        : base( logger)
    {
    }

    protected override ProcedureAsync<String> CreatePipeline()
    {
        return PipelineFactory
            .Start<String>()
            .WithLogging()
            .PipeAsync( ExecSomeAction )
            .BuildAsProcedure();
    }

    private String ExecSomeAction( IPipelineContext context, String who )
    {
        return $"Hello {who}";
    }
}

// usage
void usage( IMyCommand command )
{
    var result = await command.ExecuteAsync( "me" ); // returns "Hello me"
}
```

## Example 4

Example of a command using `PipelineFactory.Create` with an injected `IPipelineMiddlewareProvider`
to automatically apply cross-cutting hooks and wraps.

```csharp
public interface IDeleteSubscriptionCommand : ICommandFunction<string, DeleteOutput>
{
}

public class DeleteSubscriptionCommand : CommandFunction<string, DeleteOutput>, IDeleteSubscriptionCommand
{
    private readonly IPipelineMiddlewareProvider _middlewareProvider;

    public DeleteSubscriptionCommand(
        IPipelineMiddlewareProvider middlewareProvider,
        IPipelineContextFactory pipelineContextFactory,
        ILogger<DeleteSubscriptionCommand> logger )
        : base( pipelineContextFactory, logger )
    {
        _middlewareProvider = middlewareProvider;
    }

    protected override FunctionAsync<string, DeleteOutput> CreatePipeline()
    {
        return PipelineFactory.Create<string, DeleteOutput>( _middlewareProvider, builder =>
            builder
                .Pipe( ValidateId )
                .PipeAsync( LoadSubscriptionAsync )
                .ValidateAsync()
                .PipeAsync( DeleteSubscriptionAsync )
                .Pipe( Result )
        );
    }

    // ... step methods
}
```

The `Create` method applies the provider's hooks after `Start` and wraps before `Build`, so every
command that uses the provider gets consistent middleware without any extra boilerplate. See
[Middleware](middleware.md) for more details on `IPipelineMiddlewareProvider`.

## Composing Commands into Pipelines

Commands expose their inner pipeline delegate via the `PipelineFunction` property. This allows one command's
pipeline to directly compose another command's pipeline as a step, without calling `ExecuteAsync`. The key
benefit is that the pipeline context flows through naturally -- shared state, middleware, and cancellation are
preserved, and an error in a composed command halts the outer pipeline (see
[Halt-on-Error and the Boundary Model](#halt-on-error-and-the-boundary-model)).

### PipeAsync with Commands

Use `PipeAsync` to transform the pipeline value through a command's pipeline.

```csharp
public interface IFormatCommand : ICommandFunction<string, string> { }

public class FormatCommand : CommandFunction<string, string>, IFormatCommand
{
    protected override FunctionAsync<string, string> CreatePipeline()
    {
        return PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"[{arg}]" )
            .Build();
    }
}

public class ParentCommand : CommandFunction<string, string>, IParentCommand
{
    private readonly IFormatCommand _formatCommand;

    public ParentCommand( IFormatCommand formatCommand, /* ... */ )
    {
        _formatCommand = formatCommand;
    }

    protected override FunctionAsync<string, string> CreatePipeline()
    {
        return PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg.ToUpper() )
            .PipeAsync( _formatCommand )    // compose the command's pipeline directly
            .Build();
    }
}

// "hello" -> "HELLO" -> "[HELLO]"
```

### CallAsync with Commands

Use `CallAsync` to run a command's pipeline for side effects while preserving the current value.

```csharp
public interface ILogCommand : ICommandProcedure<string> { }

// In a pipeline:
.CallAsync( _logCommand )   // runs the command, preserves input value
.Pipe( ( ctx, arg ) => arg + " done" )
```

### PipeIf and CallIf with Commands

Conditionally compose a command's pipeline based on a runtime condition.

```csharp
.PipeIf( ( ctx, arg ) => arg.Length > 5, _formatCommand )
.CallIf( ( ctx, arg ) => arg.StartsWith( "log:" ), _logCommand )
```

### Halt-on-Error and the Boundary Model

A pipeline has two distinct boundaries, and they behave differently on error.

The outermost boundary returns a result and does not throw. Running a built pipeline
(or a command's `ExecuteAsync`) captures any failure on the context rather than
propagating it as an exception -- `CommandResult.Context` exposes `Success`,
`IsError`, and `Exception` as diagnostics. This is the same model as a compiler that
returns a result with diagnostics rather than throwing on a compile error. (Set
`context.Throws` to opt a specific run into rethrowing instead.)

Between steps, progression halts on error. When any step -- including a composed
command -- fails with an exception, the pipeline stops: subsequent steps do not run,
and the failure surfaces at the boundary as `IsError` / `Exception`. This prevents a
swallowed error from feeding `default` data into later steps. Because the halt reuses
the cancellation short-circuit, an errored pipeline also reports `IsCanceled == true`;
`IsError` distinguishes an error from a plain cancellation.

This halt-on-error behavior is the default. To restore the prior behavior, where
steps continue running after an error is recorded, configure it once at DI wire-up:

```csharp
services.AddPipeline( options => options.HaltOnError = false );
```

The option composes with the other `AddPipeline` overloads:

```csharp
services.AddPipeline(
    ( factoryServices, rootProvider ) => { /* register factory services */ },
    options => options.HaltOnError = false );
```

### Implicit Conversion

`CommandFunction` and `CommandProcedure` define implicit conversion operators to their respective delegate
types. When you have a concrete command reference, the implicit conversion allows direct use anywhere a
`FunctionAsync` or `ProcedureAsync` is expected.

```csharp
CommandFunction<string, string> command = /* ... */;
FunctionAsync<string, string> function = command; // implicit conversion
```
