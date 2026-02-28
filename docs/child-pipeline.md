---
layout: default
title: Child Pipelines
nav_order: 5
---

# Child Pipelines

The `PipelineFactory` library allows you to use pipelines together. Since pipelines are just functions
(`FunctionAsync<TStart, TOutput>`), they can be used as input to other pipelines. This allows you to
create complex data processing flows by reusing and chaining together multiple pipelines.

Each child pipeline is itself a Kleisli arrow -- its own `TStart` is the parent pipeline's current `TOutput`.
This is the self-similar composition at the heart of the pipeline's monadic design.

Here is an example of how to use pipelines together:

```csharp
// pipeline2: TStart=string, TOutput=string
var pipeline2 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"{arg} again!" )
    .Build();

// pipeline1: TStart=string, TOutput=string
// PipeAsync feeds pipeline1's TOutput (string) as pipeline2's TStart (string)
var pipeline1 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .PipeAsync( pipeline2 )
    .Build();

var result = await pipeline1( new PipelineContext(), "you" );

Assert.AreEqual( "hello you again!", result );
```

## Composing Commands

[Commands](command-pattern.md) are injectable wrappers around pipelines. You can compose a command's
pipeline directly into another pipeline using `PipeAsync`, `CallAsync`, `PipeIf`, or `CallIf`. The
command's pipeline is used as a step -- the pipeline context flows through without creating a new context.

```csharp
// commandB is an ICommandFunction<string, string> injected via DI

var pipeline = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .PipeAsync( commandB )          // compose command's pipeline
    .Build();
```

See [Commands](command-pattern.md) for more details.

