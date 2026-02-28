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

## Composing Commands into Pipelines

Commands expose their inner pipeline delegate via the `PipelineFunction` property. This allows one command's
pipeline to directly compose another command's pipeline as a step, without calling `ExecuteAsync`. The key
benefit is that the pipeline context flows through naturally -- shared state, middleware, exception handling,
and cancellation are all preserved.

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

### Implicit Conversion

`CommandFunction` and `CommandProcedure` define implicit conversion operators to their respective delegate
types. When you have a concrete command reference, the implicit conversion allows direct use anywhere a
`FunctionAsync` or `ProcedureAsync` is expected.

```csharp
CommandFunction<string, string> command = /* ... */;
FunctionAsync<string, string> function = command; // implicit conversion
```
