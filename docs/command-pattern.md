---
layout: default
title: Commands
nav_order: 6
---

# Command Pattern

`ICommand*` interfaces and `Command*` base classes provide a lightweight pattern for constructing injectable commands built
 around pipelines and middleware.

| Interface                              | Class                                 | Description
| -------------------------------------- | ------------------------------------- | ---------------------------------------------------
| ICommandFunction&lt;TInput,TOutput&gt; | CommandFunction&lt;TInput,TOutput&gt; | A command that takes an input and returns an output
| ICommandFunction&lt;TOutput&gt;        | CommandFunction&lt;TOutput&gt;        | A command that takes no input and returns an output
| ICommandProcedure&lt;TInput&gt;        | CommandProcedure&lt;TInput&gt;        | A command that takes an input and returns void

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