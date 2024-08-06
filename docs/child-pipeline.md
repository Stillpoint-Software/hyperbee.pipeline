---
layout: default
title: Child Pipelines
nav_order: 5
---

# Child Piplelines

The `PipelineFactory` library allows you to use pipelines together. Since pipelines are just functions, they can be used 
as input to other pipelines. This allows you to create complex data processing flows by reusing and chaining together
multiple pipelines.

Here's an example of how to use pipelines together:


```csharp
var pipeline2 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"{arg} again!" )
    .Build();

var pipeline1 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .PipeAsync( pipeline2 )
    .Build();

var result = await pipeline1( new PipelineContext(), "you" );

Assert.AreEqual( "hello you again!", result );
```

